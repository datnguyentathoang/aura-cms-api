-- Menu điều hướng chính của website.
-- Chạy được nhiều lần: mục đã có (cùng đường dẫn) thì chỉ cập nhật thứ tự, không tạo trùng. Không xoá mục nào.
-- Sau khi chạy vẫn sửa được trong admin: /admin/menu.
--
-- Trang Dự án và Bảng giá đang chưa có nội dung. Muốn tạm ẩn hai mục này cho tới khi có nội dung, chạy:
--   update menu_items set is_published = false where url in ('/portfolio', '/pricing');
-- Muốn hiện lại thì đổi false thành true.

begin;

-- Các mục cấp một trỏ tới đường dẫn cố định
insert into menu_items (label, url, sort_order)
select v.label, v.url, v.sort_order
from (values
  ('Trang chủ', '/',             1),
  ('Tính năng', '/#features',    2),
  ('Giao diện', '/#screenshots', 3),
  ('Dự án',     '/portfolio',    4),
  ('Bảng giá',  '/pricing',      5),
  ('Blog',      '/blog',         6)
) as v(label, url, sort_order)
where not exists (select 1 from menu_items m where m.parent_id is null and m.url = v.url);

update menu_items m
set sort_order = v.sort_order
from (values
  ('/',             1),
  ('/#features',    2),
  ('/#screenshots', 3),
  ('/portfolio',    4),
  ('/pricing',      5),
  ('/blog',         6)
) as v(url, sort_order)
where m.parent_id is null and m.url = v.url;

-- Nhóm "Giới thiệu" đứng cuối, có menu con
insert into menu_items (label, sort_order)
select 'Giới thiệu', 7
where not exists (select 1 from menu_items where parent_id is null and label = 'Giới thiệu');

update menu_items set sort_order = 7 where parent_id is null and label = 'Giới thiệu';

insert into menu_items (parent_id, label, page_id, sort_order)
select g.id, 'Về chúng tôi', p.id, 1
from menu_items g
join pages p on p.slug = 've-chung-toi'
where g.parent_id is null and g.label = 'Giới thiệu'
  and not exists (select 1 from menu_items c where c.parent_id = g.id and c.page_id = p.id);

insert into menu_items (parent_id, label, url, sort_order)
select g.id, 'Câu hỏi thường gặp', '/#faq', 2
from menu_items g
where g.parent_id is null and g.label = 'Giới thiệu'
  and not exists (select 1 from menu_items c where c.parent_id = g.id and c.url = '/#faq');

commit;

-- Kiểm tra: phải thấy 7 mục cấp một theo đúng thứ tự, và mục con dưới "Giới thiệu"
select coalesce(parent.label, '(cấp một)') as nhom, m.sort_order, m.label, coalesce(m.url, '/' || p.slug) as duong_dan, m.is_published
from menu_items m
left join menu_items parent on parent.id = m.parent_id
left join pages p on p.id = m.page_id
order by m.parent_id nulls first, m.sort_order;
