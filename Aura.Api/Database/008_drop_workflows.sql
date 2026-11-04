-- Xoá hẳn dữ liệu luồng nghiệp vụ. KHÔNG hoàn tác được.
--
-- Chỉ chạy SAU KHI API không còn luồng nghiệp vụ đã deploy xong trên Render.
-- API bản cũ vẫn đọc hai bảng này: xoá sớm thì API lỗi 500, trang chủ mất tính năng, ảnh chụp và câu hỏi thường gặp.
--
-- File này là tuỳ chọn. Hai bảng để lại không ảnh hưởng gì tới website, code mới không đọc chúng nữa.
-- Muốn giữ bản sao nội dung thì tải CSV của hai bảng trong Supabase Table Editor trước khi chạy.
-- Ảnh đã tải lên thư mục workflows trong Storage không bị xoá theo.

drop table if exists workflow_steps;
drop table if exists workflows;

-- Kiểm tra: phải ra 0 dòng
select tablename from pg_tables where schemaname = 'public' and tablename in ('workflows', 'workflow_steps');
