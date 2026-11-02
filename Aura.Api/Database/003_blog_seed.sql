-- Dữ liệu blog mẫu: 4 chuyên mục, 7 bài đã xuất bản, chưa có ảnh bìa (thumbnail_url = null).
-- Chạy thủ công trong Supabase SQL Editor, sau 002_blog.sql.
-- Chạy lại không tạo trùng và không ghi đè bài đã có (on conflict do nothing).
-- Lệnh gắn ảnh và lệnh xoá dữ liệu mẫu nằm trong phần ghi chú ở cuối file.

begin;

insert into categories (name, slug, description) values
  ('Quản lý kho', 'quan-ly-kho', 'Kinh nghiệm kiểm soát tồn kho, nhập xuất và kiểm kê.'),
  ('Bán hàng', 'ban-hang', 'Quy trình xử lý đơn hàng và phục vụ khách hàng.'),
  ('Công nợ và tài chính', 'cong-no-tai-chinh', 'Theo dõi công nợ, dòng tiền và lãi lỗ.'),
  ('Chuyển đổi số', 'chuyen-doi-so', 'Ứng dụng phần mềm vào vận hành doanh nghiệp.')
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
    '5 dấu hiệu doanh nghiệp đã đến lúc dùng phần mềm quản lý',
    'dau-hieu-can-dung-phan-mem-quan-ly',
    'Excel và sổ tay vẫn dùng được khi doanh nghiệp còn nhỏ. Khi số liệu bắt đầu lệch và báo cáo luôn đến muộn, đó là lúc nên nghĩ đến một hệ thống chung.',
    $html$<p>Nhiều doanh nghiệp bắt đầu với Excel, sổ tay và các nhóm chat. Cách làm này đủ dùng khi đơn hàng còn ít, nhưng dễ quá tải khi số mã hàng, số kho và số nhân viên tăng lên. Dưới đây là năm dấu hiệu cho thấy đã đến lúc cần một hệ thống quản lý chung.</p>
<h2>1. Số liệu tồn kho không ai dám chắc</h2>
<p>Trước khi báo giá, nhân viên bán hàng phải gọi xuống kho để hỏi còn hàng hay không. File tồn kho có nhiều phiên bản, mỗi người giữ một bản và không ai biết bản nào mới nhất.</p>
<h2>2. Báo cáo luôn đến muộn</h2>
<p>Cuối tháng, kế toán mất nhiều ngày để gom dữ liệu từ nhiều nguồn rồi đối chiếu. Đến khi có báo cáo thì thời điểm cần ra quyết định đã qua.</p>
<h2>3. Công nợ phải dò lại từng chứng từ</h2>
<p>Khi khách hàng hỏi còn nợ bao nhiêu, nhân viên phải lật lại hoá đơn và phiếu thu. Khoản đến hạn thường chỉ được phát hiện khi đã quá hạn.</p>
<h2>4. Cùng một dữ liệu phải nhập nhiều lần</h2>
<p>Một đơn hàng được nhập vào file bán hàng, nhập lại vào file kho rồi nhập tiếp vào file kế toán. Mỗi lần nhập lại là một lần có thể sai, và sai ở đâu cũng khó tìm.</p>
<h2>5. Công việc phụ thuộc vào vài người</h2>
<p>Chỉ một người hiểu file tính giá, chỉ một người biết vị trí hàng trong kho. Khi người đó nghỉ, công việc gần như dừng lại.</p>
<h2>Nên bắt đầu từ đâu?</h2>
<p>Không nhất thiết phải đổi toàn bộ cùng lúc. Hãy chọn phần đang gây thiệt hại nhiều nhất, thường là kho hoặc công nợ, làm sạch dữ liệu của phần đó rồi triển khai trước. Khi đội ngũ đã quen, việc mở rộng sang các phần còn lại sẽ dễ hơn.</p>
<p>Nếu doanh nghiệp của bạn đang gặp những dấu hiệu trên, hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Năm dấu hiệu cho thấy Excel và sổ tay không còn đủ, và doanh nghiệp nên bắt đầu dùng phần mềm quản lý từ đâu.',
    2,
    '2026-11-02 09:00:00+07'::timestamptz
  ),
  (
    'Vì sao tồn kho trên sổ thường lệch với thực tế?',
    'vi-sao-ton-kho-lech-thuc-te',
    'Tồn kho lệch hiếm khi do một lỗi lớn. Nguyên nhân thường là những thói quen nhỏ lặp lại mỗi ngày, và đều có thể khắc phục.',
    $html$<p>Trên sổ còn 100 thùng, ra kho chỉ đếm được 94. Chênh lệch kiểu này khiến doanh nghiệp nhận đơn mà không có hàng để giao, hoặc nhập thêm mặt hàng vẫn đang còn. Nguyên nhân thường không phải một lỗi lớn mà là những thói quen nhỏ lặp lại mỗi ngày.</p>
<h2>Những nguyên nhân thường gặp</h2>
<ul>
<li><strong>Xuất hàng trước, ghi sổ sau:</strong> hàng đã rời kho nhưng phiếu xuất được nhập vào cuối ngày, thậm chí ngày hôm sau.</li>
<li><strong>Một mặt hàng nhiều tên gọi:</strong> cùng một sản phẩm được tạo thành nhiều mã nên số tồn bị chia nhỏ.</li>
<li><strong>Nhầm đơn vị tính:</strong> nhập kho theo thùng nhưng xuất kho theo cái mà không có tỷ lệ quy đổi thống nhất.</li>
<li><strong>Không ghi nhận hàng trả về và hàng hỏng:</strong> hàng để riêng một góc nhưng vẫn nằm trong số tồn.</li>
<li><strong>Chuyển kho không có chứng từ:</strong> hàng đã sang kho khác nhưng số liệu vẫn ở kho cũ.</li>
</ul>
<h2>Cách giảm chênh lệch</h2>
<p>Trước hết, hãy thống nhất danh mục hàng hoá: mỗi sản phẩm một mã duy nhất, có đơn vị tính chuẩn và tỷ lệ quy đổi rõ ràng. Tiếp theo, đặt quy tắc không có phiếu thì không xuất hàng, kể cả với đơn gấp. Cuối cùng, tách hàng hỏng và hàng chờ xử lý sang một kho hoặc một vị trí riêng để không tính nhầm vào hàng có thể bán.</p>
<p>Khi vẫn có chênh lệch, đừng chỉ sửa số cho khớp. Hãy ghi lại nguyên nhân của từng lần điều chỉnh. Sau một thời gian, bạn sẽ thấy lỗi tập trung ở khâu nào và có thể ưu tiên sửa đúng chỗ đó.</p>
<p>Bạn muốn số tồn kho luôn được cập nhật theo từng phiếu nhập xuất? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Những thói quen nhỏ khiến tồn kho trên sổ lệch với thực tế, và các bước đơn giản để giảm chênh lệch.',
    2,
    '2026-10-29 09:00:00+07'::timestamptz
  ),
  (
    'Kiểm kê kho định kỳ: cách làm gọn mà vẫn chính xác',
    'kiem-ke-kho-dinh-ky',
    'Không cần đóng cửa kho cả ngày mới kiểm kê được. Chia hàng theo mức độ quan trọng và đếm luân phiên sẽ giúp số liệu luôn đáng tin.',
    $html$<p>Kiểm kê toàn bộ kho vào cuối năm rất tốn công và thường phải dừng nhập xuất, trong khi sai lệch đã tích tụ suốt nhiều tháng. Kiểm kê luân phiên là cách nhẹ hơn: mỗi lần chỉ đếm một phần kho, nhưng đếm đều đặn.</p>
<h2>Bước 1: Phân nhóm hàng hoá</h2>
<p>Dùng phương pháp ABC để chia hàng theo giá trị: nhóm A có giá trị cao hoặc bán chạy, nhóm B ở mức trung bình, nhóm C có giá trị thấp. Nhóm A nên được đếm thường xuyên nhất, ví dụ hằng tháng, còn nhóm C có thể đếm theo quý.</p>
<h2>Bước 2: Chốt số trước khi đếm</h2>
<p>Hãy hoàn tất các phiếu nhập xuất đang dở của khu vực sắp kiểm. Nếu vẫn phải giao hàng trong lúc đếm, hãy ghi chú riêng các mã hàng đang kiểm để tránh đếm nhầm.</p>
<h2>Bước 3: Đếm mà không nhìn số trên hệ thống</h2>
<p>Người đếm không nên biết trước số tồn trên hệ thống. Cách này giúp họ đếm thật thay vì đếm cho khớp. Với hàng giá trị cao, nên có người thứ hai đếm lại.</p>
<h2>Bước 4: Xử lý chênh lệch</h2>
<ul>
<li>Đếm lại những mã có chênh lệch trước khi kết luận.</li>
<li>Kiểm tra các phiếu nhập xuất gần nhất của mã hàng đó.</li>
<li>Ghi rõ nguyên nhân và người duyệt khi điều chỉnh số tồn.</li>
</ul>
<h2>Bước 5: Theo dõi qua từng đợt</h2>
<p>Hãy ghi lại tỷ lệ mã hàng đúng số sau mỗi đợt kiểm kê. Con số này cho biết quy trình kho đang tốt lên hay xấu đi, và giúp bạn quyết định có nên đếm dày hơn ở nhóm hàng nào hay không.</p>
<p>Cần trao đổi thêm về cách tổ chức kiểm kê cho kho của bạn? Hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Cách kiểm kê kho luân phiên theo nhóm hàng ABC để số liệu luôn đáng tin mà không phải dừng nhập xuất.',
    2,
    '2026-10-26 09:00:00+07'::timestamptz
  ),
  (
    '4 thói quen giúp thu công nợ khách hàng đúng hạn',
    'thoi-quen-thu-cong-no-dung-han',
    'Nhiều khoản nợ quá hạn không phải do khách hàng cố tình trả chậm, mà vì không ai nhắc đúng lúc hoặc chứng từ chưa rõ ràng.',
    $html$<p>Nhiều khoản nợ quá hạn không phải do khách hàng cố tình trả chậm, mà vì không ai nhắc đúng lúc hoặc chứng từ chưa rõ ràng. Bốn thói quen dưới đây không đòi hỏi công cụ phức tạp nhưng giúp dòng tiền ổn định hơn.</p>
<h2>1. Thống nhất điều khoản ngay từ đầu</h2>
<p>Hạn thanh toán, hạn mức nợ và phương thức thanh toán cần được ghi rõ trong hợp đồng hoặc đơn hàng. Đừng để mỗi nhân viên bán hàng thoả thuận một kiểu.</p>
<h2>2. Theo dõi công nợ theo tuổi nợ</h2>
<p>Thay vì chỉ nhìn tổng số tiền khách còn nợ, hãy chia theo thời gian: chưa đến hạn, quá hạn dưới 30 ngày, từ 30 đến 60 ngày và trên 60 ngày. Bảng tuổi nợ cho biết khoản nào cần xử lý trước.</p>
<h2>3. Nhắc trước khi đến hạn</h2>
<p>Một tin nhắn lịch sự vài ngày trước hạn thường dễ tiếp nhận hơn một cuộc gọi đòi nợ sau hạn. Với khoản đã quá hạn, hãy nhắc theo lịch cố định và phân công rõ ai chịu trách nhiệm theo dõi.</p>
<h2>4. Đối chiếu công nợ định kỳ</h2>
<p>Gửi bảng đối chiếu cho khách hàng hằng tháng hoặc hằng quý. Sai lệch được phát hiện sớm sẽ dễ xử lý hơn nhiều so với khi đã để lâu và đang cần thu tiền.</p>
<h2>Khi nào nên dừng giao hàng?</h2>
<p>Hãy đặt sẵn quy tắc cho những khách hàng vượt hạn mức hoặc quá hạn lâu, ví dụ cần cấp quản lý duyệt trước khi giao đơn mới. Có quy tắc từ trước sẽ giúp nhân viên bán hàng không phải tự quyết định trong từng tình huống khó.</p>
<p>Bạn muốn theo dõi công nợ và tuổi nợ trên cùng một hệ thống? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Bốn thói quen đơn giản giúp doanh nghiệp theo dõi tuổi nợ, nhắc nợ đúng lúc và thu công nợ khách hàng đúng hạn.',
    2,
    '2026-10-22 09:00:00+07'::timestamptz
  ),
  (
    'Công nợ nhà cung cấp: theo dõi thế nào để không trễ hạn?',
    'theo-doi-cong-no-nha-cung-cap',
    'Trả trễ làm giảm uy tín với nhà cung cấp, trả quá sớm lại làm căng dòng tiền. Mục tiêu là trả đúng hạn và đúng số tiền.',
    $html$<p>Trễ hạn thanh toán có thể khiến doanh nghiệp mất chiết khấu, bị siết điều kiện mua hàng hoặc bị giao chậm đúng lúc cần hàng nhất. Ngược lại, trả tiền quá sớm lại làm dòng tiền căng thẳng không cần thiết. Mục tiêu là trả đúng hạn và đúng số tiền.</p>
<h2>Đối chiếu ba chứng từ trước khi ghi nợ</h2>
<p>Trước khi ghi nhận một khoản phải trả, hãy so sánh đơn mua hàng, phiếu nhập kho và hoá đơn của nhà cung cấp. Số lượng và đơn giá phải khớp nhau. Bước này giúp phát hiện sớm hàng giao thiếu, giao sai hoặc hoá đơn tính nhầm trước khi tiền được chuyển đi.</p>
<h2>Lập lịch thanh toán theo hạn</h2>
<p>Mỗi khoản phải trả cần có ngày đến hạn, tính theo điều khoản của từng nhà cung cấp. Hằng tuần, hãy xem danh sách các khoản sắp đến hạn để chủ động chuẩn bị tiền.</p>
<h2>Đặt thứ tự ưu tiên khi dòng tiền eo hẹp</h2>
<ul>
<li>Nhà cung cấp của mặt hàng thiết yếu, khó thay thế.</li>
<li>Khoản có phí phạt nếu trả chậm hoặc có chiết khấu khi trả đúng hạn.</li>
<li>Nhà cung cấp đang chờ thanh toán để giao đơn tiếp theo.</li>
</ul>
<p>Nếu biết trước mình sẽ trả chậm, hãy báo sớm và đề xuất ngày cụ thể. Báo trước thường giữ được quan hệ tốt hơn so với im lặng.</p>
<h2>Đối chiếu số dư định kỳ</h2>
<p>Hằng tháng hoặc hằng quý, hãy đối chiếu số dư với nhà cung cấp. Các khoản tạm ứng, hàng trả lại và chiết khấu là những chỗ dễ lệch nên cần được kiểm tra kỹ.</p>
<p>Việc đối chiếu sẽ nhẹ hơn khi mua hàng, nhập kho và công nợ cùng nằm trên một hệ thống. Hãy <a href="/#contact">liên hệ với Aura</a> nếu bạn muốn tìm hiểu thêm.</p>$html$,
    'Cách đối chiếu chứng từ, lập lịch thanh toán và ưu tiên các khoản phải trả để không trễ hạn với nhà cung cấp.',
    2,
    '2026-10-19 09:00:00+07'::timestamptz
  ),
  (
    'Doanh thu tăng nhưng vẫn thiếu tiền: nên nhìn vào đâu?',
    'doanh-thu-tang-van-thieu-tien',
    'Bán được nhiều hơn không có nghĩa là có nhiều tiền hơn. Tiền có thể đang nằm ở công nợ, trong kho hoặc mất dần qua các khoản chi nhỏ.',
    $html$<p>Bán được nhiều hơn nhưng cuối tháng vẫn phải xoay tiền trả lương và trả nhà cung cấp. Đây là tình huống dễ gặp ở doanh nghiệp đang tăng trưởng, vì doanh thu và tiền thực thu là hai con số khác nhau.</p>
<h2>Tiền đang nằm ở công nợ phải thu</h2>
<p>Doanh thu được ghi nhận khi bán hàng, nhưng nếu bán chịu thì tiền chưa về. Càng bán nhiều theo kiểu trả chậm, khoản phải thu càng lớn. Hãy xem tổng nợ phải thu và bảng tuổi nợ thay vì chỉ xem doanh số.</p>
<h2>Tiền đang nằm trong kho</h2>
<p>Để đáp ứng nhu cầu tăng, doanh nghiệp thường nhập nhiều hàng hơn. Hàng chậm bán sẽ giữ tiền lại trên kệ. Hãy lọc danh sách hàng tồn lâu ngày và cân nhắc giảm nhập những mặt hàng này.</p>
<h2>Thời hạn thu và trả bị lệch</h2>
<p>Ví dụ, nếu phải trả nhà cung cấp trong 15 ngày nhưng cho khách hàng nợ 45 ngày, doanh nghiệp phải tự ứng tiền cho khoảng chênh lệch 30 ngày. Doanh số càng lớn, số tiền cần ứng càng nhiều.</p>
<h2>Lợi nhuận thấp hơn bạn nghĩ</h2>
<p>Chiết khấu, phí vận chuyển và hàng trả về có thể làm lợi nhuận thực tế thấp hơn con số ước tính. Hãy tính lãi lỗ theo từng mặt hàng hoặc từng khách hàng để biết đơn nào thực sự có lời.</p>
<h2>Ba con số nên xem hằng tuần</h2>
<ul>
<li>Tiền thực thu so với doanh số trong kỳ.</li>
<li>Nợ phải thu đã quá hạn.</li>
<li>Giá trị hàng tồn lâu ngày.</li>
</ul>
<p>Bạn muốn xem kho, công nợ và lãi lỗ trong cùng một hệ thống? Hãy <a href="/#contact">liên hệ với Aura</a> để được tư vấn.</p>$html$,
    'Vì sao doanh thu tăng mà vẫn thiếu tiền: công nợ, hàng tồn, thời hạn thu trả và lợi nhuận thực tế.',
    2,
    '2026-10-15 09:00:00+07'::timestamptz
  ),
  (
    'Chuẩn hoá quy trình từ đơn hàng đến giao hàng',
    'chuan-hoa-quy-trinh-don-hang',
    'Lỗi thường xuất hiện ở các điểm chuyển giao giữa bán hàng, kho và kế toán. Một quy trình rõ ràng giúp mọi người biết đơn đang ở đâu và ai phụ trách.',
    $html$<p>Khi mỗi nhân viên xử lý đơn theo một cách, lỗi thường xuất hiện ở các điểm chuyển giao: bán hàng đã hứa nhưng kho không còn hàng, hoặc hàng đã giao nhưng kế toán chưa biết để thu tiền. Một quy trình rõ ràng giúp mọi người biết đơn đang ở đâu và ai phụ trách.</p>
<h2>1. Tiếp nhận đơn hàng</h2>
<p>Mọi đơn, dù đến từ điện thoại, Zalo hay nhân viên kinh doanh, đều cần được đưa về một nơi theo cùng một mẫu: khách hàng, mặt hàng, số lượng, giá, ngày giao và điều khoản thanh toán.</p>
<h2>2. Kiểm tra tồn kho và hạn mức</h2>
<p>Trước khi xác nhận, hãy kiểm tra còn đủ hàng không và khách có đang vượt hạn mức công nợ không. Đơn chưa đủ điều kiện nên dừng ở bước này thay vì đến lúc soạn hàng mới phát hiện.</p>
<h2>3. Soạn và xuất hàng</h2>
<p>Kho chỉ soạn hàng theo đơn đã xác nhận. Phiếu xuất được lập ngay khi hàng rời kho để số tồn luôn được cập nhật.</p>
<h2>4. Giao hàng và xác nhận</h2>
<p>Hãy lưu lại bằng chứng đã giao, như chữ ký hoặc ảnh chụp. Nếu khách trả lại một phần, cần ghi nhận ngay để tồn kho và công nợ đều đúng.</p>
<h2>5. Ghi nhận công nợ và thu tiền</h2>
<p>Sau khi giao, khoản phải thu cần được ghi nhận với hạn thanh toán rõ ràng. Từ đó kế toán có thể theo dõi và nhắc nợ đúng lúc.</p>
<h2>Đo để cải thiện</h2>
<p>Hãy bắt đầu với ba chỉ số đơn giản: thời gian từ lúc nhận đơn đến lúc giao, tỷ lệ đơn giao đủ và đúng hạn, tỷ lệ đơn phải sửa hoặc bị trả về. Chỉ số xấu đi ở đâu thì bước đó cần được xem lại trước.</p>
<p>Cần trao đổi thêm về quy trình xử lý đơn hàng của bạn? Hãy <a href="/#contact">liên hệ với Aura</a>.</p>$html$,
    'Năm bước chuẩn hoá quy trình từ tiếp nhận đơn đến giao hàng và thu tiền, giúp giảm lỗi giữa bán hàng, kho và kế toán.',
    2,
    '2026-10-12 09:00:00+07'::timestamptz
  )
) as v(title, slug, excerpt, content, seo_description, read_minutes, published_at)
on conflict do nothing;

insert into post_categories (post_id, category_id)
select p.id, c.id
from (values
  ('dau-hieu-can-dung-phan-mem-quan-ly', 'chuyen-doi-so'),
  ('vi-sao-ton-kho-lech-thuc-te', 'quan-ly-kho'),
  ('kiem-ke-kho-dinh-ky', 'quan-ly-kho'),
  ('thoi-quen-thu-cong-no-dung-han', 'cong-no-tai-chinh'),
  ('theo-doi-cong-no-nha-cung-cap', 'cong-no-tai-chinh'),
  ('doanh-thu-tang-van-thieu-tien', 'cong-no-tai-chinh'),
  ('doanh-thu-tang-van-thieu-tien', 'ban-hang'),
  ('chuan-hoa-quy-trinh-don-hang', 'ban-hang'),
  ('chuan-hoa-quy-trinh-don-hang', 'quan-ly-kho')
) as m(post_slug, category_slug)
join posts p on lower(p.slug) = m.post_slug
join categories c on lower(c.slug) = m.category_slug
on conflict do nothing;

commit;

-- Kết quả mong đợi: 7 dòng, status = published, thumbnail_url = null.
select p.slug, p.status, p.read_minutes, p.published_at::date as ngay_dang,
       p.thumbnail_url, count(pc.category_id) as so_chuyen_muc
from posts p
left join post_categories pc on pc.post_id = p.id
where p.slug in (
  'dau-hieu-can-dung-phan-mem-quan-ly', 'vi-sao-ton-kho-lech-thuc-te',
  'kiem-ke-kho-dinh-ky', 'thoi-quen-thu-cong-no-dung-han',
  'theo-doi-cong-no-nha-cung-cap', 'doanh-thu-tang-van-thieu-tien',
  'chuan-hoa-quy-trinh-don-hang'
)
group by p.id
order by p.published_at desc;

-- ===== GẮN ẢNH BÌA SAU KHI UPLOAD =====
-- Upload vào bucket "images", sao chép URL công khai của ảnh rồi thay vào lệnh tương ứng.
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'dau-hieu-can-dung-phan-mem-quan-ly';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'vi-sao-ton-kho-lech-thuc-te';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'kiem-ke-kho-dinh-ky';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'thoi-quen-thu-cong-no-dung-han';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'theo-doi-cong-no-nha-cung-cap';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'doanh-thu-tang-van-thieu-tien';
-- update posts set thumbnail_url = 'URL_ANH', updated_at = now() where slug = 'chuan-hoa-quy-trinh-don-hang';

-- ===== XOÁ DỮ LIỆU MẪU (không hoàn tác được) =====
-- delete from posts where slug in (
--   'dau-hieu-can-dung-phan-mem-quan-ly', 'vi-sao-ton-kho-lech-thuc-te',
--   'kiem-ke-kho-dinh-ky', 'thoi-quen-thu-cong-no-dung-han',
--   'theo-doi-cong-no-nha-cung-cap', 'doanh-thu-tang-van-thieu-tien',
--   'chuan-hoa-quy-trinh-don-hang'
-- );
-- delete from categories where slug in ('quan-ly-kho', 'ban-hang', 'cong-no-tai-chinh', 'chuyen-doi-so');
