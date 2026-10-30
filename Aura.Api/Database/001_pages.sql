create table if not exists pages (
  id uuid primary key default gen_random_uuid(),
  slug text not null,
  title text not null,
  status text not null default 'draft' check (status in ('draft', 'published')),
  seo_title text,
  seo_description text,
  og_image_url text,
  published_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create unique index if not exists pages_slug_key on pages (lower(slug));
create index if not exists pages_status_idx on pages (status);

create table if not exists page_sections (
  id uuid primary key default gen_random_uuid(),
  page_id uuid not null references pages(id) on delete cascade,
  type text not null,
  content jsonb not null default '{}'::jsonb,
  sort_order int not null default 0,
  is_published boolean not null default true
);
create index if not exists page_sections_page_idx on page_sections (page_id, sort_order);

create table if not exists menu_items (
  id uuid primary key default gen_random_uuid(),
  parent_id uuid references menu_items(id) on delete cascade,
  label text not null,
  page_id uuid references pages(id) on delete set null,
  url text,
  sort_order int not null default 0,
  is_published boolean not null default true
);
create index if not exists menu_items_parent_idx on menu_items (parent_id, sort_order);

-- Same pattern as your other tables: RLS on, no policies, only the .NET API can read or write
alter table pages enable row level security;
alter table page_sections enable row level security;
alter table menu_items enable row level security;