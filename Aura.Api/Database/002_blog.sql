create table if not exists categories (
  id uuid primary key default gen_random_uuid(),
  name text not null,
  slug text not null,
  description text,
  created_at timestamptz not null default now()
);
create unique index if not exists categories_slug_key on categories (lower(slug));

create table if not exists posts (
  id uuid primary key default gen_random_uuid(),
  title text not null,
  slug text not null,
  excerpt text,
  content text not null default '',
  thumbnail_url text,
  status text not null default 'draft' check (status in ('draft', 'published', 'archived')),
  author_id uuid references admin_users(id) on delete set null,
  author_name text,
  seo_title text,
  seo_description text,
  read_minutes int not null default 1,
  published_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
create unique index if not exists posts_slug_key on posts (lower(slug));
create index if not exists posts_status_published_idx on posts (status, published_at desc);

create table if not exists post_categories (
  post_id uuid not null references posts(id) on delete cascade,
  category_id uuid not null references categories(id) on delete cascade,
  primary key (post_id, category_id)
);
create index if not exists post_categories_category_idx on post_categories (category_id);

alter table categories enable row level security;
alter table posts enable row level security;
alter table post_categories enable row level security;
