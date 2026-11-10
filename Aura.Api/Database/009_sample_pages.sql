-- Các trang mẫu: Giải pháp, Đội ngũ, Khách hàng nói gì, Tuyển dụng, Chính sách bảo mật, Điều khoản sử dụng.
--
-- Mọi trang được tạo ở dạng NHÁP: khách chưa thấy cho tới khi bạn bấm "Xuất bản" trong admin (/admin/pages).
-- Đọc lại và sửa nội dung cho đúng với công ty trước khi xuất bản, nhất là hai trang chính sách và điều khoản.
-- Trang Đội ngũ và Khách hàng nói gì lấy danh sách từ "Dữ liệu dùng chung" trong admin; danh sách trống thì phần đó tự ẩn.
--
-- Chạy được nhiều lần: trang đã có (cùng đường dẫn) thì bỏ qua, không ghi đè nội dung bạn đã sửa. Không xoá gì.
-- Bốn trang đầu được thêm vào nhóm "Giới thiệu" của menu; mục menu chỉ hiện khi trang đã xuất bản.
-- Hai trang chính sách tự hiện ở chân trang khi đã xuất bản.

begin;

-- ---------- Giải pháp ----------
with p as (
  insert into pages (slug, title, status, seo_title, seo_description)
  select 'giai-phap', 'Giải pháp', 'draft',
    'Giải pháp quản lý kho, sản xuất và bán hàng',
    'Các phân hệ của Aura ERP: mua hàng, bán hàng, đối tác, kho nguyên liệu, kho chế biến, kho thành phẩm và báo cáo lãi lỗ.'
  where not exists (select 1 from pages where lower(slug) = 'giai-phap')
  returning id
)
insert into page_sections (page_id, type, content, sort_order, is_published)
select p.id, s.type, s.content::jsonb, s.sort_order, true
from p, (values
  ('hero', $j${
    "heading": "Một hệ thống cho kho, sản xuất và bán hàng",
    "subheading": "Quản lý các kho, sản xuất, công nợ, lãi lỗ trong một hệ thống.",
    "cta_text": "Đăng ký dùng thử",
    "cta_link": "/#contact",
    "image_url": "",
    "animation": "fade"
  }$j$, 1),
  ('feature_grid', $j${
    "title": "Các phân hệ chính",
    "subtitle": "Mỗi phân hệ là một phần của cùng một hệ thống nên số liệu đi liền với nhau.",
    "items": [
      {"title": "Mua hàng", "description": "Lập phiếu nhập nguyên liệu từ nhà cung cấp và theo dõi đơn mua.", "icon": "", "image_url": ""},
      {"title": "Bán hàng", "description": "Bán hàng tại quầy, lập đơn và theo dõi công nợ của khách.", "icon": "", "image_url": ""},
      {"title": "Đối tác", "description": "Lưu thông tin nhà cung cấp và khách hàng ở một nơi.", "icon": "", "image_url": ""},
      {"title": "Kho nguyên liệu", "description": "Theo dõi nguyên liệu nhập vào, tồn kho và chuyển sang chế biến.", "icon": "", "image_url": ""},
      {"title": "Kho chế biến và lô sản xuất", "description": "Quản lý tồn kho, nhập kho, xuất kho và từng lô sản xuất.", "icon": "", "image_url": ""},
      {"title": "Kho thành phẩm", "description": "Theo dõi thành phẩm sau sản xuất, sẵn sàng để bán.", "icon": "", "image_url": ""},
      {"title": "Báo cáo lãi lỗ", "description": "Xem lãi lỗ và số liệu kho ngay trên bảng điều khiển.", "icon": "", "image_url": ""},
      {"title": "Quét mã vạch", "description": "Nhập, xuất và bán hàng nhanh hơn bằng mã vạch.", "icon": "", "image_url": ""},
      {"title": "Đồng bộ nhiều thiết bị", "description": "Số liệu được đồng bộ giữa các thiết bị bạn dùng.", "icon": "", "image_url": ""}
    ],
    "animation": "fade-up"
  }$j$, 2),
  ('cta', $j${
    "title": "Muốn xem Aura ERP chạy thực tế?",
    "text": "Để lại thông tin, chúng tôi sẽ liên hệ và hướng dẫn bạn dùng thử miễn phí.",
    "button_text": "Liên hệ tư vấn",
    "button_link": "/#contact",
    "animation": "zoom"
  }$j$, 3)
) as s(type, content, sort_order);

-- ---------- Đội ngũ ----------
with p as (
  insert into pages (slug, title, status, seo_title, seo_description)
  select 'doi-ngu', 'Đội ngũ', 'draft',
    'Đội ngũ',
    'Những người xây dựng Aura ERP và đồng hành cùng khách hàng.'
  where not exists (select 1 from pages where lower(slug) = 'doi-ngu')
  returning id
)
insert into page_sections (page_id, type, content, sort_order, is_published)
select p.id, s.type, s.content::jsonb, s.sort_order, true
from p, (values
  ('hero', $j${
    "heading": "Đội ngũ Aura",
    "subheading": "Những người xây dựng Aura ERP và đồng hành cùng doanh nghiệp trong quá trình sử dụng.",
    "cta_text": "",
    "cta_link": "",
    "image_url": "",
    "animation": "fade"
  }$j$, 1),
  ('rich_text', $j${
    "title": "Cách chúng tôi làm việc",
    "html": "<p>Trước khi triển khai, chúng tôi tìm hiểu cách kho, sản xuất và bán hàng của doanh nghiệp đang vận hành để cấu hình phần mềm cho phù hợp.</p><p>Sau khi đưa vào sử dụng, đội ngũ tiếp tục hỗ trợ để mọi người trong doanh nghiệp dùng được hằng ngày.</p>",
    "animation": "fade-up"
  }$j$, 2),
  ('team', $j${
    "title": "Thành viên",
    "subtitle": "",
    "members": [],
    "animation": "zoom"
  }$j$, 3),
  ('cta', $j${
    "title": "Cần tư vấn về phần mềm quản lý?",
    "text": "Để lại thông tin, chúng tôi sẽ liên hệ lại với bạn.",
    "button_text": "Liên hệ tư vấn",
    "button_link": "/#contact",
    "animation": "zoom"
  }$j$, 4)
) as s(type, content, sort_order);

-- ---------- Khách hàng nói gì ----------
with p as (
  insert into pages (slug, title, status, seo_title, seo_description)
  select 'khach-hang', 'Khách hàng nói gì', 'draft',
    'Khách hàng nói gì về Aura ERP',
    'Cảm nhận của các doanh nghiệp đang dùng Aura ERP để quản lý kho và bán hàng.'
  where not exists (select 1 from pages where lower(slug) = 'khach-hang')
  returning id
)
insert into page_sections (page_id, type, content, sort_order, is_published)
select p.id, s.type, s.content::jsonb, s.sort_order, true
from p, (values
  ('hero', $j${
    "heading": "Khách hàng nói gì về Aura",
    "subheading": "Cảm nhận của các doanh nghiệp đang dùng Aura ERP để quản lý kho và bán hàng.",
    "cta_text": "",
    "cta_link": "",
    "image_url": "",
    "animation": "fade"
  }$j$, 1),
  ('testimonials', $j${
    "title": "",
    "subtitle": "",
    "items": [],
    "animation": "slide-left"
  }$j$, 2),
  ('cta', $j${
    "title": "Doanh nghiệp của bạn có thể là khách hàng tiếp theo",
    "text": "Để lại thông tin, chúng tôi sẽ hướng dẫn bạn dùng thử miễn phí.",
    "button_text": "Liên hệ tư vấn",
    "button_link": "/#contact",
    "animation": "zoom"
  }$j$, 3)
) as s(type, content, sort_order);

-- ---------- Tuyển dụng ----------
with p as (
  insert into pages (slug, title, status, seo_title, seo_description)
  select 'tuyen-dung', 'Tuyển dụng', 'draft',
    'Tuyển dụng',
    'Cơ hội làm việc cùng Aura và cách gửi hồ sơ.'
  where not exists (select 1 from pages where lower(slug) = 'tuyen-dung')
  returning id
)
insert into page_sections (page_id, type, content, sort_order, is_published)
select p.id, s.type, s.content::jsonb, s.sort_order, true
from p, (values
  ('hero', $j${
    "heading": "Cùng làm việc với Aura",
    "subheading": "Chúng tôi muốn gặp những người quan tâm tới phần mềm quản lý cho doanh nghiệp.",
    "cta_text": "Gửi hồ sơ",
    "cta_link": "/#contact",
    "image_url": "",
    "animation": "fade"
  }$j$, 1),
  ('rich_text', $j${
    "title": "Vị trí và cách ứng tuyển",
    "html": "<h2>Vị trí đang tuyển</h2><p>Hiện chưa có vị trí nào đang mở. Bạn vẫn có thể gửi hồ sơ để chúng tôi liên hệ khi có vị trí phù hợp.</p><h2>Cách ứng tuyển</h2><ol><li>Chuẩn bị CV và đường dẫn tới các sản phẩm bạn đã làm, nếu có.</li><li>Gửi thông tin qua phần Liên hệ ở trang chủ, ghi rõ vị trí bạn quan tâm.</li><li>Chúng tôi sẽ phản hồi qua email bạn để lại.</li></ol>",
    "animation": "fade-up"
  }$j$, 2),
  ('cta', $j${
    "title": "Sẵn sàng gửi hồ sơ?",
    "text": "Gửi thông tin qua phần Liên hệ, chúng tôi sẽ đọc và phản hồi.",
    "button_text": "Gửi hồ sơ",
    "button_link": "/#contact",
    "animation": "zoom"
  }$j$, 3)
) as s(type, content, sort_order);

-- ---------- Chính sách bảo mật ----------
with p as (
  insert into pages (slug, title, status, seo_title, seo_description)
  select 'chinh-sach-bao-mat', 'Chính sách bảo mật', 'draft',
    'Chính sách bảo mật',
    'Website thu thập thông tin gì, dùng để làm gì và quyền của bạn đối với thông tin đó.'
  where not exists (select 1 from pages where lower(slug) = 'chinh-sach-bao-mat')
  returning id
)
insert into page_sections (page_id, type, content, sort_order, is_published)
select p.id, 'rich_text', $j${
    "title": "Chính sách bảo mật",
    "html": "<p>Trang này giải thích website thu thập thông tin gì, dùng để làm gì và bạn có những quyền gì.</p><h2>Thông tin chúng tôi thu thập</h2><ul><li>Họ tên, email và nội dung bạn gửi qua form liên hệ.</li><li>Nội dung bạn trao đổi với trợ lý trên website, nếu bạn dùng trợ lý.</li><li>Họ tên, số điện thoại hoặc email bạn để lại khi đề nghị được tư vấn trong khung trò chuyện.</li></ul><h2>Mục đích sử dụng</h2><p>Chúng tôi dùng các thông tin này để trả lời câu hỏi, tư vấn và hỗ trợ bạn dùng thử sản phẩm. Chúng tôi không bán thông tin của bạn cho bên khác.</p><h2>Cookie</h2><p>Website dùng một cookie để giữ cuộc trò chuyện của bạn với trợ lý giữa các lần tải trang. Cookie này không dùng để quảng cáo.</p><h2>Lưu trữ và bảo vệ</h2><p>Thông tin được lưu trên hạ tầng máy chủ chúng tôi sử dụng và chỉ quản trị viên của website được xem.</p><h2>Quyền của bạn</h2><p>Bạn có thể yêu cầu xem, sửa hoặc xoá thông tin đã gửi bằng cách liên hệ với chúng tôi qua phần Liên hệ ở trang chủ.</p><h2>Thay đổi chính sách</h2><p>Khi chính sách thay đổi, chúng tôi sẽ cập nhật nội dung trên trang này.</p>",
    "animation": "none"
  }$j$::jsonb, 1, true
from p;

-- ---------- Điều khoản sử dụng ----------
with p as (
  insert into pages (slug, title, status, seo_title, seo_description)
  select 'dieu-khoan-su-dung', 'Điều khoản sử dụng', 'draft',
    'Điều khoản sử dụng',
    'Điều khoản khi truy cập và sử dụng website.'
  where not exists (select 1 from pages where lower(slug) = 'dieu-khoan-su-dung')
  returning id
)
insert into page_sections (page_id, type, content, sort_order, is_published)
select p.id, 'rich_text', $j${
    "title": "Điều khoản sử dụng",
    "html": "<p>Khi truy cập website này, bạn đồng ý với các điều khoản dưới đây.</p><h2>Nội dung trên website</h2><p>Nội dung, hình ảnh và tài liệu trên website dùng để giới thiệu sản phẩm Aura ERP. Bạn không được sao chép cho mục đích thương mại khi chưa được chúng tôi đồng ý.</p><h2>Thông tin sản phẩm</h2><p>Thông tin về tính năng và bảng giá có thể thay đổi. Vui lòng liên hệ để có thông tin mới nhất trước khi quyết định.</p><h2>Trợ lý trên website</h2><p>Câu trả lời của trợ lý dựa trên nội dung đã đăng trên website và chỉ mang tính tham khảo.</p><h2>Liên kết tới website khác</h2><p>Website có thể có liên kết tới trang của bên thứ ba. Chúng tôi không chịu trách nhiệm về nội dung của các trang đó.</p><h2>Liên hệ</h2><p>Nếu có câu hỏi về các điều khoản này, vui lòng gửi qua phần Liên hệ ở trang chủ.</p>",
    "animation": "none"
  }$j$::jsonb, 1, true
from p;

-- ---------- Menu: thêm vào nhóm "Giới thiệu" (chỉ hiện khi trang đã xuất bản) ----------
insert into menu_items (parent_id, label, page_id, sort_order)
select g.id, v.label, p.id, v.sort_order
from menu_items g
cross join (values
  ('giai-phap', 'Giải pháp', 2),
  ('doi-ngu', 'Đội ngũ', 3),
  ('khach-hang', 'Khách hàng nói gì', 4),
  ('tuyen-dung', 'Tuyển dụng', 5)
) as v(slug, label, sort_order)
join pages p on lower(p.slug) = v.slug
where g.parent_id is null and g.label = 'Giới thiệu'
  and not exists (select 1 from menu_items c where c.page_id = p.id);

-- Đưa "Câu hỏi thường gặp" xuống cuối nhóm, chỉ khi bạn chưa đổi thứ tự của nó
update menu_items c
set sort_order = 6
from menu_items g
where c.parent_id = g.id and g.parent_id is null and g.label = 'Giới thiệu'
  and c.url = '/#faq' and c.sort_order = 2;

commit;

-- Kiểm tra: các trang mẫu, số section và mục menu
select p.slug, p.title, p.status,
  (select count(*) from page_sections s where s.page_id = p.id) as so_section,
  (select count(*) from menu_items m where m.page_id = p.id) as trong_menu
from pages p
where p.slug in ('giai-phap', 'doi-ngu', 'khach-hang', 'tuyen-dung', 'chinh-sach-bao-mat', 'dieu-khoan-su-dung')
order by p.slug;
