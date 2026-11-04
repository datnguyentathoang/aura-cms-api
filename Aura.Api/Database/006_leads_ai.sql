-- Phase 4: tin nhắn liên hệ thành khách hàng tiềm năng (lead), và dữ liệu cho trợ lý AI.
-- Chạy được nhiều lần; không xoá dữ liệu có sẵn. API đang chạy bản cũ vẫn hoạt động sau khi chạy file này.
-- Phải chạy TRƯỚC khi deploy API mới: API mới đọc các cột lead, thiếu cột thì form liên hệ sẽ lỗi.

create table if not exists chat_sessions (
  id uuid primary key default gen_random_uuid(),
  token_hash text not null,
  page_url text,
  message_count int not null default 0,
  created_at timestamptz not null default now(),
  last_message_at timestamptz not null default now()
);
create index if not exists chat_sessions_last_idx on chat_sessions (last_message_at desc);

create table if not exists chat_messages (
  id uuid primary key default gen_random_uuid(),
  session_id uuid not null references chat_sessions(id) on delete cascade,
  role text not null check (role in ('user', 'assistant')),
  content text not null,
  sources jsonb not null default '[]'::jsonb,
  provider text,
  created_at timestamptz not null default now()
);
create index if not exists chat_messages_session_idx on chat_messages (session_id, created_at);
create index if not exists chat_messages_role_time_idx on chat_messages (role, created_at);

-- Kiến thức bổ sung do admin nhập. Bài viết, trang, dự án... đã xuất bản được đọc trực tiếp từ bảng gốc
create table if not exists knowledge_documents (
  id uuid primary key default gen_random_uuid(),
  title text not null,
  content text not null,
  url text,
  sort_order int not null default 0,
  is_published boolean not null default true,
  created_at timestamptz not null default now()
);

-- Chỉ có một dòng. Trợ lý mặc định tắt cho tới khi admin bật
create table if not exists ai_settings (
  id int primary key default 1 check (id = 1),
  is_enabled boolean not null default false,
  bot_name text not null default 'Trợ lý Aura',
  welcome_message text not null default 'Xin chào! Mình là trợ lý của Aura. Bạn muốn tìm hiểu điều gì về phần mềm quản lý kho và bán hàng?',
  fallback_message text not null default 'Mình chưa có thông tin chắc chắn về việc này. Bạn để lại thông tin để đội ngũ Aura tư vấn trực tiếp nhé.',
  daily_message_limit int not null default 300,
  session_message_limit int not null default 30,
  updated_at timestamptz not null default now()
);
insert into ai_settings (id) values (1) on conflict (id) do nothing;

-- Lead từ chatbot có thể chỉ có số điện thoại nên email không còn bắt buộc
alter table contact_messages alter column email drop not null;
alter table contact_messages add column if not exists phone text;
alter table contact_messages add column if not exists status text not null default 'new';
alter table contact_messages add column if not exists source text not null default 'contact_form';
alter table contact_messages add column if not exists notes text;
alter table contact_messages add column if not exists chat_session_id uuid references chat_sessions(id) on delete set null;
alter table contact_messages add column if not exists updated_at timestamptz;

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'contact_messages_status_check') then
    alter table contact_messages add constraint contact_messages_status_check
      check (status in ('new', 'contacted', 'qualified', 'won', 'lost'));
  end if;
end $$;

-- Tin đã xử lý trước đây coi như đã liên hệ
update contact_messages set status = 'contacted' where is_handled and status = 'new';

create index if not exists contact_messages_status_idx on contact_messages (status, created_at desc);
create index if not exists contact_messages_chat_idx on contact_messages (chat_session_id);

-- Same pattern as your other tables: RLS on, no policies, only the .NET API can read or write
alter table chat_sessions enable row level security;
alter table chat_messages enable row level security;
alter table knowledge_documents enable row level security;
alter table ai_settings enable row level security;

-- Kiểm tra: phải ra 4 dòng và rowsecurity đều là true
select tablename, rowsecurity
from pg_tables
where schemaname = 'public'
  and tablename in ('chat_sessions', 'chat_messages', 'knowledge_documents', 'ai_settings')
order by tablename;
