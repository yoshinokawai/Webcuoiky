# 🌐 VT-Wiki — Tài liệu kỹ thuật dự án (Technical Project Documentation)

Tài liệu này cung cấp cái nhìn chi tiết nhất về cách mã nguồn của dự án **VT-Wiki** hoạt động. Đây là hướng dẫn dành cho các lập trình viên để nắm bắt logic nghiệp vụ, cấu trúc dữ liệu và các thành phần bảo mật.

---

## 🏗️ 1. Kiến trúc Hệ thống (System Architecture)

Dự án được xây dựng trên nền tảng **ASP.NET Core 10 (MVC)**. Đây là một ứng dụng web hiện đại với cấu trúc phân lớp rõ ràng.

### Khởi chạy và Middleware (`Program.cs`)
File `Program.cs` đóng vai trò là "trái tim" của ứng dụng, nơi cấu hình các dịch vụ và đường ống xử lý yêu cầu (HTTP request pipeline):

- **Dependency Injection (DI)**: Các dịch vụ như `ApplicationDbContext`, `IFileService` (Cloudinary) và `IActivityService` được đăng ký vào container DI để sử dụng xuyên suốt các Controller.
- **Localization Middleware**: Cấu hình hỗ trợ 3 ngôn ngữ (`en`, `vi`, `ja`). Hệ thống sẽ tự động nhận diện ngôn ngữ dựa trên Cookie của người dùng.
- **Authentication & Authorization**: Sử dụng `CookieAuthentication` để quản lý phiên đăng nhập của người dùng. Chúng tôi tự triển khai logic này thay vì dùng ASP.NET Identity để tăng tính tùy biến và hiệu năng.

```csharp
// Cấu hình Authentication trong Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
```

---

## 📊 2. Layer Dữ liệu & Models (Data Layer)

Hệ thống sử dụng **Entity Framework Core 10** với mô hình **Code-First**.

### Chi tiết các Entity (Models):

#### **User.cs** (Quản trị người dùng)
- `Username`, `Email`: Định danh duy nhất.
- `PasswordHash`: Lưu mật khẩu đã mã hóa.
- `SecurityPin`: Mã khôi phục 6 chữ số (Rất quan trọng cho luồng khôi phục mật khẩu).
- `Role`: Phân quyền (Admin, Editor, User).

#### **Vtuber.cs** (Thực thể trung tâm)
- `Name`, `Lore`, `DebutDate`: Thông tin cơ bản.
- `ViewCount`: Tự động tăng mỗi khi trang chi tiết được truy cập.
- `AgencyId`: Khóa ngoại liên kết với Agency (nếu là Talent thuộc công ty).
- `Status`: Trạng thái nội dung (`Approved`, `Pending`).

#### **Discussion.cs** & **DiscussionReply.cs** (Diễn đàn)
- `Category`: Phân loại (General, Gaming, FanArt...).
- `LikeCount`, `ReplyCount`: Lưu trữ số lượng tương tác để hiển thị nhanh (Denormalization để tối ưu query).
- Quan hệ 1-N giữa bài đăng và các bình luận.

---

## 🔐 3. Logics Xử lý cốt lõi (Core Business Logic)

### A. Hệ thống Bảo mật & Auth (`AccountController`)
Chúng tôi sử dụng thuật toán **SHA-256** kết hợp với **Salt** để bảo vệ dữ liệu người dùng.

- **Hash & Salt**: Mỗi mật khẩu được nối thêm chuỗi `"VTWiki_Salt_"` trước khi băm. Điều này ngăn chặn việc tra cứu mật khẩu từ các bản hack dữ liệu phổ biến.
- **Luồng khôi phục mật khẩu (PIN-based)**:
  1. Người dùng nhập Email/Username.
  2. Hệ thống yêu cầu nhập mã PIN 6 số.
  3. Nếu mã PIN khớp, người dùng được chuyển đến trang đặt lại mật khẩu (`ResetPassword`).

```csharp
// Logic băm mật khẩu thực tế
private static string HashPassword(string password) {
    using (var sha256 = SHA256.Create()) {
        var saltedPassword = "VTWiki_Salt_" + password;
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(bytes);
    }
}
```

### B. Logic Wiki & Tìm kiếm thông minh (`WikiController`)
- **API Tìm kiếm nhanh (`SearchSuggest`)**: Trả về dữ liệu dạng JSON bao gồm cả VTuber và Agency, cho phép hiển thị kết quả ngay lập tức trên thanh tìm kiếm (Instant Search).
- **Phân loại Talent**: Sử dụng LINQ (`Where`, `Include`) để lọc Talent theo Agency hoặc Talent độc lập (Independent).

### C. Logic Diễn đàn & Tương tác (`ForumController`)
- **Cơ chế Like**: Sử dụng bảng `DiscussionLikes` để lưu trữ cặp `(UserId, DiscussionId)`. Trước khi tăng `LikeCount`, hệ thống kiểm tra xem bản ghi này đã tồn tại chưa để ngăn "spam like".

---

## 🛠️ 4. Dịch vụ & Tiện ích (Services)

### Cloudinary CDN (`FileService.cs`)
Dự án tích hợp trực tiếp với Cloudinary API để quản lý hình ảnh:
- **Upload**: Chuyển đổi file từ `IFormFile` thành luồng dữ liệu (Stream) và gửi lên Cloudinary.
- **Optimization**: Tự động áp dụng Transformation (Resize, nén chất lượng) để đảm bảo trang web load nhanh ngay cả khi có nhiều ảnh độ phân giải cao.

### Real-time Activity Feed (`ActivityService.cs`)
Hệ thống theo dõi mọi biến động của cộng đồng:
- Mỗi hành động (tạo bài, sửa wiki) sẽ gọi `LogActivityAsync`.
- Dữ liệu này được lưu vào bảng `Activities` và hiển thị tại trang chủ dưới dạng "Hoạt động gần đây".

---

## 🌍 5. Đa ngôn ngữ & ViewComponents

### Localization
Toàn bộ chuỗi ký tự trên UI được quản lý trong `Resources/SharedResource.{culture}.resx`. 
- Trong View: Sử dụng `@inject IViewLocalizer Localizer`.
- Trong Controller: Sử dụng `IStringLocalizer<SharedResource>`.

### ViewComponents (UI Reusability)
Thay vì dùng Partial Views đơn giản, chúng tôi dùng **ViewComponents** cho các thành phần có logic phức tạp:
- `LatestNewsViewComponent`: Lấy 5 tin tức mới nhất từ DB.
- `DiscussionSidebarViewComponent`: Thống kê danh mục diễn đàn.

---

## 📁 6. Sơ đồ thư mục kỹ thuật (Detailed Tree)

```text
WebWikiForum/
├── Controllers/         # Logic điều hướng và xử lý nghiệp vụ
├── Data/                # DbContext, Migrations (Quản lý CSDL)
├── Models/              # Các lớp đối tượng chuẩn (Entity)
├── ViewModels/          # Lớp dữ liệu trung gian cho Form và UI
├── Services/            # Cloudinary Upload, Activity Logging
├── Resources/           # Tài liệu bản dịch đa ngôn ngữ
├── ViewComponents/      # Các thành phần UI có logic riêng
├── Views/               # Razor Templates (Giao diện người dùng)
├── wwwroot/             # CSS, JS, Images (Chứa logic Instant Search)
└── Program.cs           # Cấu hình Services, Middleware và Routing
```

---

*Tài liệu này được biên soạn để phục vụ mục đích phát triển và bảo trì dự án VT-Wiki.*
