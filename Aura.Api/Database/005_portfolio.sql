-- Phase 3: dự án (case study) kèm thư viện ảnh, đội ngũ, đánh giá khách hàng và bảng giá.
-- Chạy được nhiều lần; không xoá hay sửa dữ liệu có sẵn.

create table if not exists projects (
  id uuid primary key default gen_random_uuid(),
  title text not null,
  slug text not null,
  client_name text,
  industry text,
  summary text,
  content text not null default '',
  thumbnail_url text,
  status text not null default 'draft' check (status in ('draft', 'published')),
  seo_title text,
  seo_description text,
  sort_order int not null default 0,
  published_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create unique index if not exists projects_slug_key on projects (lower(slug));
create index if not exists projects_status_idx on projects (status, sort_order);

create table if not exists project_images (
  id uuid primary key default gen_random_uuid(),
  project_id uuid not null references projects(id) on delete cascade,
  image_url text not null,
  caption text,
  sort_order int not null default 0
);
create index if not exists project_images_project_idx on project_images (project_id, sort_order);

create table if not exists team_members (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  role text,
  bio text,
  photo_url text,
  sort_order int not null default 0,
  is_published boolean not null default true,
  created_at timestamptz not null default now()
);

create table if not exists testimonials (
  id uuid primary key default gen_random_uuid(),
  quote text not null,
  name text not null,
  role text,
  avatar_url text,
  sort_order int not null default 0,
  is_published boolean not null default true,
  created_at timestamptz not null default now()
);

-- price là chữ vì có thể là "9.900.000đ" hoặc "Liên hệ"
create table if not exists pricing_plans (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  price text,
  period text,
  description text,
  features text[] not null default '{}',
  highlighted boolean not null default false,
  cta_text text,
  cta_link text,
  sort_order int not null default 0,
  is_published boolean not null default true,
  created_at timestamptz not null default now()
);

-- Same pattern as your other tables: RLS on, no policies, only the .NET API can read or write
alter table projects enable row level security;
alter table project_images enable row level security;
alter table team_members enable row level security;
alter table testimonials enable row level security;
alter table pricing_plans enable row level security;

-- Kiểm tra: phải ra 5 dòng và rowsecurity đều là true
select tablename, rowsecurity
from pg_tables
where schemaname = 'public'
  and tablename in ('projects', 'project_images', 'team_members', 'testimonials', 'pricing_plans')
order by tablename;
