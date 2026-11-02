-- Thêm dữ liệu blog mẫu: 1 chuyên mục mới và 17 bài đã xuất bản, chưa có ảnh bìa (thumbnail_url = null).
-- Chạy thủ công trong Supabase SQL Editor, sau 003_blog_seed.sql.
-- Cùng 7 bài của file 003, tổng cộng 24 bài chia thành 3 trang (10, 10 và 4 bài).
-- Chạy lại không tạo trùng và không ghi đè bài đã có (on conflict do nothing).
-- Lệnh gắn ảnh và lệnh xoá dữ liệu mẫu nằm trong phần ghi chú ở cuối file.

begin;

insert into categories (name, slug, description) values
  ('Sản xuất', 'san-xuat', 'Định mức, hao hụt và kế hoạch sản xuất.')
on conflict do nothing;

insert into posts (
  title, slug, excerpt, content, status, author_name, seo_description,
  read_minutes, published_at, created_at, updated_at
)
select
  title, slug, excerpt, content, 'published', 'Aura', seo_description,
  read_minutes, published_at, published_at, published_at
from (values
  (
    'FIFO và FEFO: nên chọn cách xuất kho nào?',
    'fifo-va-fefo-chon-cach-xuat-kho',
    'Quy tắc xuất kho ảnh hưởng trực tiếp đến lượng hàng hết hạn, hàng lỗi mốt và số tiền bị giữ trong kho. Hãy chọn theo đặc điểm của mặt hàng.',
    $html$<p>Trong kho, hàng nào xuất trước nghe có vẻ là chuyện nhỏ. Thực tế, quy tắc xuất kho ảnh hưởng trực tiếp đến lượng hàng hết hạn, hàng lỗi mốt và số tiền bị giữ trong kho. Hai cách phổ biến nhất là FIFO và FEFO.</p>
<h2>FIFO: nhập trước, xuất trước</h2>
<p>Lô hàng nhập kho sớm nhất được xuất đi trước. Cách này phù hợp với hàng không có hạn dùng rõ ràng nhưng có thể cũ đi theo thời gian, như linh kiện, vật tư hoặc hàng thời trang.</p>
<h2>FEFO: hết hạn trước, xuất trước</h2>
<p>Lô có hạn dùng gần nhất được xuất đi trước, kể cả khi lô đó nhập sau. Đây là lựa chọn thường thấy với thực phẩm, dược phẩm và mỹ phẩm, nơi hạn dùng quan trọng hơn ngày nhập.</p>
<h2>Áp dụng trong thực tế</h2>
<ul>
<li><strong>Quản lý theo lô:</strong> mỗi lần nhập cần ghi số lô, ngày nhập và hạn dùng nếu có.</li>
<li><strong>Bố trí kệ hợp lý:</strong> lô cần xuất trước phải nằm ở vị trí dễ lấy nhất.</li>
<li><strong>Ghi rõ lô trên phiếu xuất:</strong> nhân viên kho biết chính xác cần lấy lô nào thay vì tự chọn.</li>
<li><strong>Cảnh báo sớm:</strong> lập danh sách các lô sắp hết hạn để có kế hoạch bán trước khi quá muộn.</li>
</ul>
<p>Lưu ý rằng quy tắc xuất hàng trong kho và phương pháp tính giá xuất kho trong kế toán là hai việc khác nhau. Khi chọn phương pháp tính giá, bạn nên trao đổi thêm với kế toán của doanh nghiệp.</p>
<p>Bạn cần theo dõi hàng theo lô và hạn dùng? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'So sánh hai quy tắc xuất kho FIFO và FEFO, mặt hàng nào phù hợp với cách nào và cách áp dụng trong thực tế.',
    2,
    '2026-10-09 09:00:00+07'::timestamptz
  ),
  (
    'Bảng tuổi nợ là gì và đọc thế nào cho đúng?',
    'bang-tuoi-no-la-gi',
    'Tổng công nợ chỉ cho biết khách còn nợ bao nhiêu. Bảng tuổi nợ cho biết khoản nào đang rủi ro và cần được xử lý trước.',
    $html$<p>Hai khách hàng cùng nợ 200 triệu đồng có thể có mức rủi ro rất khác nhau: một khoản chưa đến hạn, khoản kia đã quá hạn nhiều tháng. Bảng tuổi nợ giúp bạn nhìn ra sự khác biệt đó.</p>
<h2>Bảng tuổi nợ là gì?</h2>
<p>Đây là báo cáo chia công nợ của từng khách hàng theo thời gian quá hạn. Các nhóm thường dùng là chưa đến hạn, quá hạn từ 1 đến 30 ngày, từ 31 đến 60 ngày, từ 61 đến 90 ngày và trên 90 ngày.</p>
<h2>Đọc bảng như thế nào?</h2>
<ul>
<li><strong>Xem cột quá hạn lâu nhất trước:</strong> khoản để càng lâu thường càng khó thu.</li>
<li><strong>So sánh qua các tháng:</strong> nếu tỷ trọng nợ quá hạn tăng dần, đó là dấu hiệu cần chú ý dù tổng công nợ chưa đổi.</li>
<li><strong>Tìm khách hàng chiếm tỷ trọng lớn:</strong> rủi ro tập trung vào vài khách hàng cần được theo dõi riêng.</li>
</ul>
<h2>Dùng bảng tuổi nợ để làm gì?</h2>
<p>Bảng tuổi nợ có thể dùng để lập danh sách ưu tiên thu tiền mỗi tuần, quyết định tạm dừng bán chịu với khách hàng quá hạn lâu và xem lại hạn mức công nợ.</p>
<h2>Điều kiện để số liệu đúng</h2>
<p>Mỗi hoá đơn cần có hạn thanh toán rõ ràng và mỗi khoản thu cần được gắn đúng vào hoá đơn. Nếu tiền thu về chỉ được trừ vào số dư chung, bạn sẽ khó biết khoản nào còn lại và đã quá hạn bao lâu.</p>
<p>Bạn muốn xem tuổi nợ mà không phải tổng hợp thủ công? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Giải thích bảng tuổi nợ, cách đọc theo từng nhóm thời gian và cách dùng để ưu tiên thu tiền mỗi tuần.',
    2,
    '2026-10-06 09:00:00+07'::timestamptz
  ),
  (
    'Định mức nguyên vật liệu: nền tảng của quản lý sản xuất',
    'dinh-muc-nguyen-vat-lieu',
    'Không có định mức thì khó biết giá thành thực tế và càng khó phát hiện hao hụt. Đây là những việc cần làm để xây dựng một bộ định mức dùng được.',
    $html$<p>Định mức nguyên vật liệu, còn gọi là BOM, cho biết cần bao nhiêu nguyên liệu và vật tư để làm ra một đơn vị thành phẩm. Không có định mức, bạn khó biết giá thành thực tế, khó lập kế hoạch mua hàng và cũng khó phát hiện hao hụt.</p>
<h2>Cách xây dựng định mức</h2>
<ol>
<li>Liệt kê toàn bộ nguyên liệu, bao bì và vật tư phụ cho từng sản phẩm.</li>
<li>Dùng đơn vị tính thống nhất với kho để tránh sai khi quy đổi.</li>
<li>Đo thực tế trên một vài lô sản xuất thay vì chỉ ước lượng.</li>
<li>Ghi riêng tỷ lệ hao hụt cho phép để dễ so sánh về sau.</li>
</ol>
<h2>Những sai lầm thường gặp</h2>
<ul>
<li>Bỏ sót vật tư nhỏ như nhãn, keo hoặc thùng đóng gói.</li>
<li>Chép định mức của sản phẩm gần giống mà không kiểm tra lại.</li>
<li>Thay đổi công thức nhưng không cập nhật định mức.</li>
</ul>
<h2>Giữ định mức luôn đúng</h2>
<p>Mỗi lần thay đổi nên có phiên bản và ngày áp dụng, nhờ đó các lệnh sản xuất cũ vẫn tra lại được. Sau mỗi lệnh sản xuất, hãy so sánh lượng dùng thực tế với định mức. Chênh lệch lặp lại nhiều lần có thể có nghĩa là định mức đã không còn đúng.</p>
<p>Cần trao đổi thêm về quản lý sản xuất? Hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Định mức nguyên vật liệu là gì, cách xây dựng từng bước và cách giữ định mức luôn sát với thực tế sản xuất.',
    2,
    '2026-10-02 09:00:00+07'::timestamptz
  ),
  (
    'Quản lý bảng giá: tránh bán sai giá khi có nhiều nhóm khách hàng',
    'quan-ly-bang-gia-ban-hang',
    'Khi có giá đại lý, giá bán lẻ và nhiều chương trình chiết khấu, bán sai giá là lỗi rất dễ xảy ra. Quản lý tập trung sẽ giúp giảm rủi ro này.',
    $html$<p>Khi doanh nghiệp có giá đại lý, giá bán lẻ và nhiều chương trình chiết khấu, bán sai giá là lỗi rất dễ xảy ra. Sai giá không chỉ làm giảm lợi nhuận mà còn gây khó xử khi phải điều chỉnh lại với khách hàng.</p>
<h2>Chỉ có một nguồn giá</h2>
<p>Bảng giá nên được lưu ở một nơi và có người chịu trách nhiệm cập nhật. Khi mỗi nhân viên kinh doanh giữ một file riêng, sớm muộn cũng sẽ có người báo giá cũ.</p>
<h2>Chia khách hàng theo nhóm</h2>
<p>Thay vì đặt giá riêng cho từng khách, hãy chia thành các nhóm như bán lẻ, đại lý và khách hàng dự án. Chỉ những trường hợp thật sự đặc biệt mới cần giá riêng.</p>
<h2>Luôn có ngày hiệu lực</h2>
<p>Mỗi lần đổi giá cần có ngày bắt đầu áp dụng. Nhờ đó, đơn hàng đã chốt vẫn giữ giá cũ và bạn có thể tra lại giá tại bất kỳ thời điểm nào.</p>
<h2>Giới hạn quyền chiết khấu</h2>
<p>Hãy quy định mức chiết khấu tối đa nhân viên được tự quyết. Vượt mức này thì cần cấp quản lý duyệt, để mọi khoản giảm giá lớn đều có người chịu trách nhiệm.</p>
<h2>So sánh giá bán với giá vốn</h2>
<p>Khi giá nhập tăng, bảng giá cũ có thể khiến một số mặt hàng gần như không còn lãi. Hãy rà soát định kỳ thay vì đợi đến cuối kỳ mới phát hiện.</p>
<p>Bạn đang cần sắp xếp lại bảng giá cho nhiều nhóm khách hàng? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Cách quản lý bảng giá theo nhóm khách hàng, ngày hiệu lực và quyền chiết khấu để tránh bán sai giá.',
    2,
    '2026-09-29 09:00:00+07'::timestamptz
  ),
  (
    'Làm sạch dữ liệu trước khi chuyển sang phần mềm mới',
    'lam-sach-du-lieu-truoc-khi-trien-khai',
    'Phần mềm tốt không sửa được dữ liệu xấu. Dành thời gian làm sạch danh mục, tồn kho và công nợ trước khi chuyển đổi sẽ tránh được nhiều rắc rối về sau.',
    $html$<p>Phần mềm mới không tự sửa được dữ liệu cũ. Nếu đưa nguyên các file đang trùng lặp và thiếu sót vào hệ thống, bạn sẽ chỉ có cùng những vấn đề đó trên một công cụ mới. Dưới đây là những phần nên làm sạch trước.</p>
<h2>Danh mục hàng hoá</h2>
<p>Gộp các mã trùng, thống nhất cách đặt tên và đơn vị tính, đồng thời loại bỏ những mã đã ngừng kinh doanh và không còn tồn. Đây thường là phần tốn nhiều công nhất nhưng cũng có giá trị nhất.</p>
<h2>Khách hàng và nhà cung cấp</h2>
<p>Kiểm tra các khách hàng bị nhập nhiều lần với tên gần giống nhau. Bổ sung những thông tin cần thiết như mã số thuế, địa chỉ, người liên hệ và điều khoản thanh toán.</p>
<h2>Số dư đầu kỳ</h2>
<ul>
<li><strong>Tồn kho:</strong> kiểm kê thực tế ngay trước ngày chuyển đổi, không chép từ file cũ.</li>
<li><strong>Công nợ:</strong> đối chiếu với khách hàng và nhà cung cấp, nên nhập theo từng hoá đơn còn nợ.</li>
</ul>
<h2>Chọn thời điểm chuyển đổi</h2>
<p>Hãy chọn đầu kỳ và tránh mùa cao điểm. Ngày chuyển đổi cần được thông báo rõ để mọi bộ phận chốt số liệu cùng lúc.</p>
<h2>Giao người chịu trách nhiệm</h2>
<p>Mỗi nhóm dữ liệu cần có một người phụ trách và một hạn hoàn thành. Nếu không, việc làm sạch dữ liệu rất dễ bị đẩy lùi cho đến sát ngày triển khai.</p>
<p>Bạn đang chuẩn bị chuyển đổi hệ thống? Hãy <a href="/#contact">liên hệ với Aura</a> để trao đổi cụ thể.</p>$html$,
    'Những dữ liệu cần làm sạch trước khi triển khai phần mềm quản lý: danh mục hàng hoá, khách hàng, tồn kho và công nợ đầu kỳ.',
    2,
    '2026-09-25 09:00:00+07'::timestamptz
  ),
  (
    'Mã vạch trong kho: nên bắt đầu từ đâu?',
    'ma-vach-trong-kho-bat-dau-tu-dau',
    'Mã vạch giúp giảm nhập tay và nhầm mã hàng, nhưng chỉ phát huy tác dụng khi danh mục hàng hoá đã gọn gàng. Đây là cách bắt đầu từng bước.',
    $html$<p>Mã vạch giúp nhân viên kho không phải gõ tay mã hàng, nhờ đó giảm nhầm lẫn và xử lý phiếu nhanh hơn. Tuy nhiên, nhiều doanh nghiệp mua máy quét trước rồi mới nhận ra dữ liệu hàng hoá chưa sẵn sàng. Thứ tự hợp lý nên như sau.</p>
<h2>1. Làm sạch danh mục hàng hoá</h2>
<p>Mỗi sản phẩm chỉ nên có một mã. Nếu một mặt hàng đang tồn tại dưới nhiều tên, hãy gộp lại trước khi in tem.</p>
<h2>2. Dùng mã có sẵn khi có thể</h2>
<p>Nhiều sản phẩm đã có mã vạch của nhà sản xuất in trên bao bì. Bạn chỉ cần gắn mã đó vào danh mục. Những mặt hàng chưa có mã mới cần tự tạo và in tem nội bộ.</p>
<h2>3. Thử trên một nhóm hàng</h2>
<p>Hãy chọn một nhóm hàng bán chạy hoặc hay nhầm lẫn để chạy thử trong vài tuần. Cách này giúp phát hiện sớm các vấn đề như tem dễ bong, mã khó quét hoặc thao tác còn rườm rà.</p>
<h2>4. Quy định rõ khi nào phải quét</h2>
<p>Hãy thống nhất quét ở những bước nào: nhập kho, soạn hàng, xuất kho hay kiểm kê. Nếu chỗ quét chỗ không, số liệu vẫn sẽ lệch như trước.</p>
<h2>5. Chuẩn bị phương án khi có sự cố</h2>
<p>Tem hỏng, máy quét hết pin hay mất mạng đều có thể xảy ra. Nhân viên cần biết phải làm gì trong những trường hợp đó để công việc không bị dừng và dữ liệu không bị bỏ sót.</p>
<p>Bạn đang tính đưa mã vạch vào kho? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Các bước áp dụng mã vạch trong kho: làm sạch danh mục, dùng mã có sẵn, chạy thử trên một nhóm hàng và quy định khi nào phải quét.',
    2,
    '2026-09-22 09:00:00+07'::timestamptz
  ),
  (
    'Tính lãi lỗ theo từng mặt hàng: vì sao cần và làm thế nào?',
    'tinh-lai-lo-theo-mat-hang',
    'Kết quả chung của công ty có thể che giấu những mặt hàng đang bán gần như không có lời. Tính theo từng mặt hàng giúp bạn biết nên đẩy mạnh và nên dừng bán gì.',
    $html$<p>Công ty có lãi không có nghĩa là mọi mặt hàng đều có lời. Một vài sản phẩm bán rất chạy nhưng lãi thấp có thể đang kéo kết quả chung xuống mà không ai để ý.</p>
<h2>Tính như thế nào?</h2>
<p>Công thức cơ bản là <strong>lãi gộp = giá bán - chiết khấu - giá vốn</strong>. Ví dụ, một mặt hàng có giá bán 100.000 đồng, chiết khấu 10.000 đồng và giá vốn 70.000 đồng thì lãi gộp là 20.000 đồng cho mỗi đơn vị.</p>
<h2>Những khoản dễ bị bỏ sót</h2>
<ul>
<li>Chi phí vận chuyển khi mua hàng về kho.</li>
<li>Hàng hỏng và hàng bị trả lại.</li>
<li>Thưởng và chiết khấu cuối kỳ cho đại lý.</li>
</ul>
<h2>Đọc kết quả ra sao?</h2>
<ul>
<li><strong>Bán chạy, lãi cao:</strong> ưu tiên giữ đủ hàng.</li>
<li><strong>Bán chạy, lãi thấp:</strong> xem lại giá bán và chính sách chiết khấu.</li>
<li><strong>Bán chậm, lãi cao:</strong> tìm cách giới thiệu đến nhiều khách hàng hơn.</li>
<li><strong>Bán chậm, lãi thấp:</strong> cân nhắc giảm nhập hoặc dừng bán.</li>
</ul>
<p>Cách ghi nhận giá vốn cho báo cáo chính thức cần tuân theo chế độ kế toán mà doanh nghiệp áp dụng, vì vậy bạn nên thống nhất số liệu với kế toán trước khi đưa ra quyết định.</p>
<p>Bạn muốn xem lãi lỗ và tồn kho trên cùng một hệ thống? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Cách tính lãi gộp theo từng mặt hàng, những khoản dễ bị bỏ sót và cách đọc kết quả để ra quyết định bán hàng.',
    2,
    '2026-09-18 09:00:00+07'::timestamptz
  ),
  (
    'Xử lý hàng trả lại sao cho kho và công nợ đều đúng',
    'xu-ly-hang-tra-lai',
    'Hàng trả lại thường được xử lý vội và ghi nhận thiếu, khiến tồn kho và công nợ cùng sai. Một quy trình ngắn gọn có thể tránh được điều này.',
    $html$<p>Hàng trả lại là nghiệp vụ rất dễ bị bỏ sót. Hàng đã về kho nhưng chưa có phiếu, hoặc khách đã trả hàng nhưng công nợ vẫn giữ nguyên. Một quy trình ngắn gọn sẽ giúp kho và công nợ cùng đúng.</p>
<h2>1. Tiếp nhận có chứng từ</h2>
<p>Mỗi lần trả hàng cần gắn với đơn bán gốc, ghi rõ mặt hàng, số lượng và lý do. Không có đơn gốc, bạn sẽ khó kiểm tra giá đã bán và điều kiện đổi trả.</p>
<h2>2. Kiểm tra tình trạng hàng</h2>
<p>Hãy phân loại thành hàng còn bán được, hàng cần sửa chữa và hàng hỏng. Chỉ hàng còn bán được mới quay lại khu bán hàng, phần còn lại chuyển sang khu riêng.</p>
<h2>3. Ghi nhận vào kho</h2>
<p>Lập phiếu nhập hàng trả ngay khi nhận hàng. Đừng để hàng nằm ở góc kho nhiều ngày rồi mới ghi sổ, vì khi đó số tồn sẽ không còn đúng.</p>
<h2>4. Điều chỉnh công nợ hoặc hoàn tiền</h2>
<p>Nếu khách còn nợ, hãy ghi giảm công nợ tương ứng. Nếu khách đã thanh toán, hãy hoàn tiền hoặc trừ vào đơn sau theo thoả thuận. Các chứng từ và hoá đơn liên quan nên được xử lý cùng kế toán.</p>
<h2>5. Thống kê lý do trả hàng</h2>
<p>Nếu một mặt hàng thường bị trả vì cùng một lý do, hãy xử lý từ gốc: chất lượng, đóng gói, mô tả sản phẩm hoặc khâu soạn hàng.</p>
<p>Cần trao đổi thêm về quy trình đổi trả của bạn? Hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Quy trình năm bước xử lý hàng trả lại: tiếp nhận, kiểm tra, nhập kho, điều chỉnh công nợ và thống kê nguyên nhân.',
    2,
    '2026-09-15 09:00:00+07'::timestamptz
  ),
  (
    'Theo dõi hao hụt trong sản xuất từ những bước đơn giản',
    'theo-doi-hao-hut-san-xuat',
    'Hao hụt là điều khó tránh trong sản xuất, nhưng không đo được thì không giảm được. Hãy bắt đầu bằng việc so sánh lượng dùng thực tế với định mức.',
    $html$<p>Hao hụt là điều khó tránh trong sản xuất, nhưng nhiều xưởng chỉ biết mình đã hao hụt khi cuối kỳ kiểm kê thấy thiếu nguyên liệu. Lúc đó đã quá muộn để biết chuyện xảy ra ở lô nào. Bốn bước sau giúp phát hiện sớm hơn.</p>
<h2>Bước 1: Ghi đúng lượng xuất và lượng dư</h2>
<p>Mỗi lệnh sản xuất cần ghi lượng nguyên liệu xuất ra, lượng dùng không hết trả về kho, số thành phẩm đạt và số phế phẩm. Nếu phần dư không được nhập lại, số liệu hao hụt sẽ bị sai.</p>
<h2>Bước 2: So sánh với định mức</h2>
<p>Ví dụ, theo định mức một lô hàng cần 100 kg nguyên liệu nhưng thực tế dùng 108 kg. Phần chênh lệch 8 kg, tức 8% so với định mức, là con số cần được giải thích.</p>
<h2>Bước 3: Tìm nguyên nhân</h2>
<ul>
<li>Nguyên liệu đầu vào không đạt chất lượng.</li>
<li>Máy móc chưa được căn chỉnh hoặc cần bảo trì.</li>
<li>Thao tác chưa đúng hoặc chưa thống nhất giữa các ca.</li>
<li>Ghi chép sai, hoặc định mức đã không còn phù hợp.</li>
</ul>
<h2>Bước 4: Theo dõi theo lô, ca và máy</h2>
<p>Khi có dữ liệu theo từng lô, ca làm việc và máy, bạn sẽ dễ nhận ra quy luật. Hao hụt chỉ tăng ở một ca hoặc một máy thường dễ khắc phục hơn nhiều so với vấn đề chung của cả xưởng.</p>
<p>Bạn muốn kết nối số liệu sản xuất với kho? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Bốn bước theo dõi hao hụt trong sản xuất: ghi nhận lượng xuất và lượng dư, so sánh với định mức, tìm nguyên nhân và theo dõi theo lô.',
    2,
    '2026-09-11 09:00:00+07'::timestamptz
  ),
  (
    'Cách xác định mức tồn kho tối thiểu cho từng mặt hàng',
    'muc-ton-kho-toi-thieu',
    'Nhập quá nhiều thì giữ vốn, nhập quá ít thì thiếu hàng để bán. Một công thức đơn giản có thể giúp bạn biết khi nào cần đặt thêm hàng.',
    $html$<p>Nhập quá nhiều thì tiền nằm trong kho, nhập quá ít thì mất đơn vì thiếu hàng. Mức tồn kho tối thiểu, hay điểm đặt hàng lại, giúp trả lời câu hỏi: còn bao nhiêu hàng thì cần đặt thêm?</p>
<h2>Công thức đơn giản</h2>
<p><strong>Điểm đặt hàng = lượng bán trung bình mỗi ngày × số ngày chờ hàng về + tồn kho an toàn.</strong></p>
<p>Ví dụ, một mặt hàng bán trung bình 10 cái mỗi ngày, nhà cung cấp giao sau 7 ngày và bạn muốn giữ thêm 20 cái để dự phòng. Khi đó, điểm đặt hàng là 10 × 7 + 20 = 90 cái. Khi tồn kho xuống còn 90 cái, bạn nên đặt thêm.</p>
<h2>Xác định tồn kho an toàn</h2>
<p>Tồn kho an toàn dùng để đề phòng khi bán nhiều hơn dự kiến hoặc hàng về trễ. Mặt hàng có nhu cầu thất thường hoặc nhà cung cấp hay giao chậm cần mức dự phòng cao hơn. Mặt hàng ổn định có thể giữ ít hơn.</p>
<h2>Những lưu ý khi áp dụng</h2>
<ul>
<li>Dùng số liệu bán của một giai đoạn đủ dài và loại bỏ các đơn bất thường.</li>
<li>Xem lại định kỳ, đặc biệt với hàng bán theo mùa.</li>
<li>Ưu tiên nhóm hàng quan trọng trước, thay vì làm cho mọi mã cùng lúc.</li>
</ul>
<p>Công thức chỉ chính xác khi số tồn kho và số liệu bán hàng đáng tin. Nếu bạn cần trao đổi thêm, hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Công thức tính điểm đặt hàng và tồn kho an toàn, kèm ví dụ và những lưu ý khi áp dụng cho từng nhóm hàng.',
    2,
    '2026-09-08 09:00:00+07'::timestamptz
  ),
  (
    'Những số liệu bán hàng nên xem mỗi tuần',
    'so-lieu-ban-hang-nen-xem-moi-tuan',
    'Không cần một báo cáo dài. Năm con số dưới đây thường đủ để biết tình hình bán hàng đang tốt lên hay cần can thiệp sớm.',
    $html$<p>Báo cáo càng dài càng ít người đọc. Với nhiều đội bán hàng, năm con số dưới đây thường đủ để biết tình hình đang tốt lên hay cần can thiệp sớm.</p>
<h2>1. Doanh số so với mục tiêu</h2>
<p>Đừng chỉ xem tổng doanh số của tuần. Hãy so với mục tiêu tháng để biết cần bán thêm bao nhiêu trong thời gian còn lại.</p>
<h2>2. Số đơn và giá trị trung bình mỗi đơn</h2>
<p>Doanh số giảm có thể vì ít đơn hơn hoặc vì mỗi đơn nhỏ hơn. Tách hai con số này ra sẽ biết nên xử lý ở đâu.</p>
<h2>3. Mặt hàng bán chạy và bán chậm</h2>
<p>Mặt hàng bán chạy cần được kiểm tra tồn kho để tránh thiếu hàng. Mặt hàng bán chậm cần được xem lại giá hoặc cách giới thiệu.</p>
<h2>4. Lãi gộp</h2>
<p>Doanh số tăng nhờ giảm giá mạnh chưa chắc đã là tin tốt. Hãy xem lãi gộp cùng với doanh số để tránh bán nhiều mà lời ít.</p>
<h2>5. Đơn chưa giao và đơn quá hẹn</h2>
<p>Đơn trễ hẹn ảnh hưởng đến trải nghiệm của khách hàng và việc thu tiền. Danh sách này cần có người phụ trách xử lý ngay trong tuần.</p>
<p>Hãy dùng cùng một mẫu báo cáo mỗi tuần và so sánh với tuần trước. Xu hướng qua nhiều tuần thường nói lên nhiều điều hơn con số của một tuần riêng lẻ.</p>
<p>Bạn muốn có các báo cáo này mà không phải tổng hợp thủ công? Hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Năm số liệu bán hàng nên theo dõi hằng tuần: doanh số, số đơn, mặt hàng bán chạy, lãi gộp và đơn quá hẹn.',
    2,
    '2026-09-04 09:00:00+07'::timestamptz
  ),
  (
    'Giúp nhân viên quen với phần mềm mới mà không gây xáo trộn',
    'giup-nhan-vien-quen-phan-mem-moi',
    'Nhiều dự án phần mềm không thành công vì nhân viên không dùng, chứ không phải vì thiếu tính năng. Vài cách sau giúp đội ngũ chuyển đổi nhẹ nhàng hơn.',
    $html$<p>Một phần mềm dù tốt đến đâu cũng không mang lại kết quả nếu nhân viên vẫn quay lại Excel. Việc triển khai vì vậy không chỉ là cài đặt hệ thống mà còn là thay đổi thói quen làm việc.</p>
<h2>Giải thích lý do thay đổi</h2>
<p>Hãy nói rõ phần mềm giúp gì cho công việc của từng người, ví dụ không phải nhập lại dữ liệu hay không phải gọi điện hỏi tồn kho. Mọi người dễ đồng thuận hơn khi thấy lợi ích cụ thể.</p>
<h2>Chọn người đi đầu ở mỗi bộ phận</h2>
<p>Mỗi bộ phận nên có một người được đào tạo trước để làm đầu mối hỗ trợ đồng nghiệp. Nhân viên thường thoải mái hỏi người trong nhóm hơn.</p>
<h2>Đào tạo theo đúng công việc</h2>
<p>Thay vì giới thiệu mọi tính năng, hãy hướng dẫn theo những việc mà mỗi người làm hằng ngày. Thủ kho cần biết cách lập phiếu nhập xuất, còn nhân viên bán hàng cần biết cách tạo đơn.</p>
<h2>Chạy song song trong thời gian ngắn</h2>
<p>Dùng song song cách cũ và cách mới trong một thời gian ngắn giúp kiểm tra số liệu, nhưng đừng kéo dài vì mọi người sẽ phải làm hai lần. Hãy chốt một ngày dừng hẳn cách làm cũ và thông báo trước.</p>
<h2>Lắng nghe phản hồi</h2>
<p>Trong những tuần đầu, hãy thu thập vướng mắc và xử lý sớm. Những lỗi nhỏ được giải quyết nhanh sẽ giúp mọi người tin tưởng vào hệ thống mới.</p>
<p>Bạn đang lên kế hoạch triển khai? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Các cách giúp nhân viên làm quen với phần mềm quản lý mới: giải thích lý do, đào tạo theo công việc và chốt ngày dừng cách làm cũ.',
    2,
    '2026-09-01 09:00:00+07'::timestamptz
  ),
  (
    'Đặt hạn mức công nợ cho khách hàng thế nào cho hợp lý?',
    'dat-han-muc-cong-no-khach-hang',
    'Hạn mức quá thấp có thể làm mất đơn, quá cao lại làm tăng rủi ro nợ khó đòi. Vài tiêu chí đơn giản giúp bạn quyết định có cơ sở hơn.',
    $html$<p>Hạn mức quá thấp có thể làm mất đơn, quá cao lại làm tăng rủi ro nợ khó đòi. Không có con số chung cho mọi doanh nghiệp, nhưng vài tiêu chí sau sẽ giúp bạn quyết định có cơ sở hơn.</p>
<h2>Nên dựa vào những gì?</h2>
<ul>
<li><strong>Lịch sử thanh toán:</strong> khách có thường trả đúng hạn hay không.</li>
<li><strong>Doanh số trung bình:</strong> khách thường mua bao nhiêu mỗi tháng.</li>
<li><strong>Thời gian hợp tác:</strong> khách mới nên bắt đầu ở mức thấp hơn.</li>
<li><strong>Thời hạn thanh toán:</strong> được nợ càng lâu thì số dư nợ thường càng lớn.</li>
</ul>
<h2>Một cách tính tham khảo</h2>
<p>Bạn có thể lấy doanh số trung bình mỗi tháng nhân với số tháng được nợ. Ví dụ, khách mua trung bình 100 triệu đồng mỗi tháng và được nợ 30 ngày thì hạn mức khởi điểm có thể vào khoảng 100 triệu đồng. Sau đó, hãy điều chỉnh theo mức độ tin cậy của khách hàng.</p>
<h2>Khi khách vượt hạn mức</h2>
<p>Hãy quy định trước cách xử lý: cảnh báo, cần quản lý duyệt hoặc tạm dừng giao hàng cho đến khi thanh toán. Điều quan trọng là áp dụng nhất quán cho mọi khách hàng.</p>
<h2>Xem lại định kỳ</h2>
<p>Khách luôn trả đúng hạn có thể được tăng hạn mức, còn khách thường xuyên trễ hạn nên được giảm. Hãy xem lại vài lần mỗi năm thay vì đặt một lần rồi để đó.</p>
<p>Cần trao đổi thêm về chính sách công nợ? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Tiêu chí đặt hạn mức công nợ cho khách hàng, cách tính tham khảo và cách xử lý khi khách hàng vượt hạn mức.',
    2,
    '2026-08-28 09:00:00+07'::timestamptz
  ),
  (
    'Sắp xếp vị trí hàng trong kho để soạn hàng nhanh hơn',
    'sap-xep-vi-tri-hang-trong-kho',
    'Nhân viên kho thường mất nhiều thời gian cho việc đi lại và tìm hàng. Sắp xếp lại vị trí theo tần suất xuất có thể rút ngắn thời gian này mà không cần mở rộng kho.',
    $html$<p>Trong một ca làm việc, nhân viên kho có thể mất nhiều thời gian cho việc đi lại và tìm hàng. Muốn làm nhanh hơn, không phải lúc nào cũng cần mở rộng kho. Nhiều khi chỉ cần sắp xếp lại vị trí.</p>
<h2>Đặt hàng bán chạy ở nơi dễ lấy</h2>
<p>Hãy thống kê số lần xuất của từng mặt hàng. Những mã được xuất thường xuyên nên nằm gần khu đóng gói và ở độ cao dễ với tay. Hàng ít xuất có thể đặt ở xa hoặc trên cao.</p>
<h2>Đặt mã cho từng vị trí</h2>
<p>Hãy đặt tên theo khu, dãy, kệ và tầng, ví dụ A-02-03. Khi mỗi vị trí có mã riêng, nhân viên mới cũng tìm được hàng mà không cần hỏi người có kinh nghiệm.</p>
<h2>Để hàng hay bán cùng nhau ở gần nhau</h2>
<p>Nếu hai sản phẩm thường xuất hiện trong cùng một đơn, đặt chúng gần nhau sẽ giảm quãng đường khi soạn hàng.</p>
<h2>Tách rõ các khu vực</h2>
<ul>
<li>Khu nhận hàng và kiểm tra.</li>
<li>Khu lưu trữ chính.</li>
<li>Khu soạn hàng và đóng gói.</li>
<li>Khu hàng hỏng và hàng chờ xử lý.</li>
</ul>
<p>Nhu cầu bán hàng thay đổi theo thời gian, nên hãy xem lại cách bố trí sau mỗi vài tháng thay vì giữ nguyên mãi.</p>
<p>Bạn muốn quản lý tồn kho theo từng vị trí? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Cách sắp xếp vị trí hàng trong kho theo tần suất xuất, đặt mã vị trí và phân khu để soạn hàng nhanh và ít nhầm hơn.',
    2,
    '2026-08-25 09:00:00+07'::timestamptz
  ),
  (
    'Từ đơn hàng đến lệnh sản xuất: kết nối bán hàng và xưởng',
    'tu-don-hang-den-lenh-san-xuat',
    'Khi bán hàng và sản xuất thiếu kết nối, doanh nghiệp dễ hứa ngày giao không thực tế. Một luồng rõ ràng giúp hai bộ phận cùng nhìn vào một số liệu.',
    $html$<p>Nhân viên kinh doanh hứa giao trong tuần, trong khi xưởng đang kín lịch và kho thiếu nguyên liệu. Tình huống này thường xảy ra khi bán hàng và sản xuất làm việc trên hai nguồn thông tin khác nhau. Một luồng rõ ràng có thể gồm năm bước sau.</p>
<h2>1. Kiểm tra tồn thành phẩm</h2>
<p>Khi nhận đơn, hãy xem kho còn bao nhiêu thành phẩm chưa được giữ cho đơn khác. Chỉ phần còn thiếu mới cần đưa vào sản xuất.</p>
<h2>2. Tính nhu cầu nguyên vật liệu</h2>
<p>Dựa vào định mức, hãy tính lượng nguyên liệu cần dùng rồi so với tồn kho. Phần thiếu sẽ trở thành yêu cầu mua hàng, kèm ngày cần có.</p>
<h2>3. Xác định ngày giao khả thi</h2>
<p>Ngày giao cần tính đến thời gian chờ nguyên liệu và lịch của xưởng. Báo đúng ngày ngay từ đầu tốt hơn là hứa sớm rồi phải xin dời.</p>
<h2>4. Phát lệnh sản xuất</h2>
<p>Lệnh sản xuất nên ghi rõ sản phẩm, số lượng, định mức áp dụng, ngày cần hoàn thành và đơn hàng liên quan để có thể truy ngược khi cần.</p>
<h2>5. Cập nhật tiến độ và nhập kho</h2>
<p>Hãy cập nhật tiến độ để bộ phận bán hàng tự xem được thay vì phải gọi xuống xưởng. Khi hoàn thành, lập phiếu nhập kho thành phẩm để số tồn và đơn hàng cùng được cập nhật.</p>
<p>Cần trao đổi thêm về cách kết nối bán hàng và sản xuất? Hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Năm bước kết nối đơn hàng với sản xuất: kiểm tra tồn, tính nguyên vật liệu, xác định ngày giao, phát lệnh và cập nhật tiến độ.',
    2,
    '2026-08-21 09:00:00+07'::timestamptz
  ),
  (
    'Xử lý hàng tồn lâu ngày trước khi hàng mất giá',
    'xu-ly-hang-ton-lau-ngay',
    'Hàng nằm kho lâu không chỉ chiếm chỗ mà còn giữ tiền của doanh nghiệp. Xử lý sớm thường ít tốn kém hơn so với chờ đến khi hàng không còn giá trị.',
    $html$<p>Hàng nằm kho lâu không chỉ chiếm chỗ. Nó còn giữ tiền của doanh nghiệp, tốn chi phí lưu trữ và có nguy cơ hư hỏng hoặc lỗi thời. Xử lý sớm thường ít tốn kém hơn so với chờ đến khi hàng không còn giá trị.</p>
<h2>Bước 1: Lập danh sách hàng chậm luân chuyển</h2>
<p>Hãy lọc các mặt hàng không phát sinh xuất kho trong một khoảng thời gian nhất định, ví dụ 90 ngày. Sắp xếp theo giá trị tồn để biết nên xử lý mã nào trước.</p>
<h2>Bước 2: Tìm nguyên nhân</h2>
<p>Có thể do nhập quá nhiều, dự báo sai, sản phẩm đã có phiên bản mới hoặc nhân viên bán hàng không biết là còn hàng. Mỗi nguyên nhân cần một cách xử lý khác nhau.</p>
<h2>Bước 3: Chọn cách xử lý</h2>
<ul>
<li><strong>Bán kèm:</strong> ghép với sản phẩm bán chạy để tăng khả năng tiêu thụ.</li>
<li><strong>Giảm giá có kế hoạch:</strong> đặt mức giảm và thời hạn cụ thể.</li>
<li><strong>Trả lại nhà cung cấp:</strong> nếu hợp đồng cho phép.</li>
<li><strong>Thanh lý:</strong> khi chi phí giữ hàng đã vượt quá giá trị có thể thu lại.</li>
</ul>
<h2>Bước 4: Ngăn tình trạng lặp lại</h2>
<p>Hãy xem lại cách đặt hàng của những mặt hàng này, đồng thời theo dõi danh sách hàng chậm luân chuyển định kỳ thay vì chỉ xem vào cuối năm.</p>
<p>Bạn muốn biết tiền đang nằm ở những mặt hàng nào? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Các bước xử lý hàng tồn lâu ngày: lập danh sách, tìm nguyên nhân, chọn cách giải phóng hàng và ngăn tình trạng lặp lại.',
    2,
    '2026-08-18 09:00:00+07'::timestamptz
  ),
  (
    'Quản lý nhiều kho: những điều dễ bị bỏ sót',
    'quan-ly-nhieu-kho',
    'Khi có từ hai kho trở lên, biết tổng số hàng là chưa đủ. Bạn còn cần biết hàng đang ở đâu, đang được chuyển đi đâu và ai được phép điều chuyển.',
    $html$<p>Khi có từ hai kho trở lên, biết tổng số hàng là chưa đủ. Một kho có thể đang thiếu trong khi kho khác còn dư, và khách hàng vẫn nhận được câu trả lời là hết hàng. Dưới đây là những điều dễ bị bỏ sót.</p>
<h2>Hàng đang trên đường chuyển</h2>
<p>Hàng đã rời kho A nhưng chưa tới kho B thì đang ở đâu? Nếu không có trạng thái riêng cho hàng đang chuyển, số liệu sẽ lệch trong suốt thời gian vận chuyển. Hãy tách rõ hai bước xuất chuyển và nhận chuyển, mỗi bước có người xác nhận.</p>
<h2>Quyền của từng kho</h2>
<p>Nhân viên nào được xem và được thao tác trên kho nào cần được phân định rõ. Điều này giúp dễ xác định trách nhiệm khi có chênh lệch.</p>
<h2>Danh mục hàng dùng chung</h2>
<p>Các kho phải dùng cùng một mã hàng và đơn vị tính. Nếu mỗi kho tự đặt mã, bạn sẽ không thể cộng tồn kho hay so sánh giữa các kho.</p>
<h2>Quy tắc chọn kho xuất</h2>
<p>Khi một đơn có thể giao từ nhiều kho, hãy đặt sẵn quy tắc ưu tiên, ví dụ kho gần khách nhất hoặc kho còn đủ hàng. Có quy tắc sẽ tránh việc nhân viên tự chọn và chuyển hàng qua lại không cần thiết.</p>
<h2>Kiểm kê khi có hàng đang chuyển</h2>
<p>Khi kiểm kê, hãy chú ý các phiếu chuyển kho còn dở dang để tránh một lô hàng bị đếm hai lần hoặc không được đếm lần nào.</p>
<p>Bạn đang vận hành nhiều kho và muốn có số liệu chung? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Những điểm cần chú ý khi quản lý nhiều kho: hàng đang chuyển, phân quyền, danh mục dùng chung và quy tắc chọn kho xuất.',
    2,
    '2026-08-14 09:00:00+07'::timestamptz
  )
) as v(title, slug, excerpt, content, seo_description, read_minutes, published_at)
on conflict do nothing;

insert into post_categories (post_id, category_id)
select p.id, c.id
from (values
  ('fifo-va-fefo-chon-cach-xuat-kho', 'quan-ly-kho'),
  ('bang-tuoi-no-la-gi', 'cong-no-tai-chinh'),
  ('dinh-muc-nguyen-vat-lieu', 'san-xuat'),
  ('quan-ly-bang-gia-ban-hang', 'ban-hang'),
  ('lam-sach-du-lieu-truoc-khi-trien-khai', 'chuyen-doi-so'),
  ('ma-vach-trong-kho-bat-dau-tu-dau', 'quan-ly-kho'),
  ('ma-vach-trong-kho-bat-dau-tu-dau', 'chuyen-doi-so'),
  ('tinh-lai-lo-theo-mat-hang', 'cong-no-tai-chinh'),
  ('xu-ly-hang-tra-lai', 'ban-hang'),
  ('xu-ly-hang-tra-lai', 'quan-ly-kho'),
  ('theo-doi-hao-hut-san-xuat', 'san-xuat'),
  ('theo-doi-hao-hut-san-xuat', 'quan-ly-kho'),
  ('muc-ton-kho-toi-thieu', 'quan-ly-kho'),
  ('so-lieu-ban-hang-nen-xem-moi-tuan', 'ban-hang'),
  ('giup-nhan-vien-quen-phan-mem-moi', 'chuyen-doi-so'),
  ('dat-han-muc-cong-no-khach-hang', 'ban-hang'),
  ('dat-han-muc-cong-no-khach-hang', 'cong-no-tai-chinh'),
  ('sap-xep-vi-tri-hang-trong-kho', 'quan-ly-kho'),
  ('tu-don-hang-den-lenh-san-xuat', 'san-xuat'),
  ('tu-don-hang-den-lenh-san-xuat', 'ban-hang'),
  ('xu-ly-hang-ton-lau-ngay', 'quan-ly-kho'),
  ('xu-ly-hang-ton-lau-ngay', 'cong-no-tai-chinh'),
  ('quan-ly-nhieu-kho', 'quan-ly-kho')
) as m(post_slug, category_slug)
join posts p on lower(p.slug) = m.post_slug
join categories c on lower(c.slug) = m.category_slug
on conflict do nothing;

commit;

-- Kết quả mong đợi khi đã chạy file 003 và không có bài nào khác: so_bai = 24, so_trang = 3.
select count(*) as so_bai, ceil(count(*) / 10.0) as so_trang
from posts
where status = 'published' and published_at <= now();

-- ===== GẮN ẢNH BÌA SAU KHI UPLOAD =====
-- Upload vào bucket "images", sao chép URL công khai của ảnh rồi thay vào lệnh tương ứng.
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'fifo-va-fefo-chon-cach-xuat-kho';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'bang-tuoi-no-la-gi';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'dinh-muc-nguyen-vat-lieu';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'quan-ly-bang-gia-ban-hang';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'lam-sach-du-lieu-truoc-khi-trien-khai';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'ma-vach-trong-kho-bat-dau-tu-dau';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'tinh-lai-lo-theo-mat-hang';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'xu-ly-hang-tra-lai';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'theo-doi-hao-hut-san-xuat';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'muc-ton-kho-toi-thieu';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'so-lieu-ban-hang-nen-xem-moi-tuan';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'giup-nhan-vien-quen-phan-mem-moi';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'dat-han-muc-cong-no-khach-hang';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'sap-xep-vi-tri-hang-trong-kho';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'tu-don-hang-den-lenh-san-xuat';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'xu-ly-hang-ton-lau-ngay';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'quan-ly-nhieu-kho';

-- ===== XOÁ DỮ LIỆU MẪU CỦA FILE NÀY (không hoàn tác được) =====
-- delete from posts where slug in (
--   'fifo-va-fefo-chon-cach-xuat-kho', 'bang-tuoi-no-la-gi', 'dinh-muc-nguyen-vat-lieu',
--   'quan-ly-bang-gia-ban-hang', 'lam-sach-du-lieu-truoc-khi-trien-khai', 'ma-vach-trong-kho-bat-dau-tu-dau',
--   'tinh-lai-lo-theo-mat-hang', 'xu-ly-hang-tra-lai', 'theo-doi-hao-hut-san-xuat',
--   'muc-ton-kho-toi-thieu', 'so-lieu-ban-hang-nen-xem-moi-tuan', 'giup-nhan-vien-quen-phan-mem-moi',
--   'dat-han-muc-cong-no-khach-hang', 'sap-xep-vi-tri-hang-trong-kho', 'tu-don-hang-den-lenh-san-xuat',
--   'xu-ly-hang-ton-lau-ngay', 'quan-ly-nhieu-kho'
-- );
-- delete from categories where slug = 'san-xuat';
