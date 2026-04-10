# 🌐 VT-Wiki — VTuber Encyclopedia & Community Forum

> Một nền tảng wiki cộng đồng và diễn đàn thảo luận dành cho người hâm mộ VTuber, được xây dựng bằng ASP.NET Core MVC.

---

## 📖 Giới thiệu

**VT-Wiki** là ứng dụng web đa ngôn ngữ cho phép người dùng tra cứu thông tin về các VTuber (cả độc lập lẫn thuộc agency), đọc tin tức mới nhất, và tham gia cộng đồng thảo luận sôi nổi. Hệ thống hỗ trợ phân quyền người dùng, upload ảnh đại diện qua Cloudinary, và giao diện được bản địa hóa theo 3 ngôn ngữ: Anh, Việt, Nhật.

---

## ✨ Tính năng chính

### 🔍 Wiki VTuber
- Xem danh sách VTuber theo **agency** (Hololive, NIJISANJI, VShojo, ...) hoặc **VTuber độc lập**
- Tra cứu thông tin chi tiết: tên, ngày debut, ngày sinh, lore, ngôn ngữ, khu vực, tags
- Theo dõi **lượt xem** từng trang VTuber
- Liên kết nhanh đến kênh YouTube của VTuber
- Trang **Virtual Events** (sự kiện trực tuyến) và **Translation Tools** (công cụ dịch thuật)

### 🏢 Quản lý Agency
- Danh sách các agency VTuber với logo, khu vực, lĩnh vực và số lượng talent
- Trang chi tiết agency liên kết với danh sách VTuber thuộc agency đó

### 💬 Community Forum
- Diễn đàn thảo luận theo chủ đề: **General, Music, Gaming, Lore, FanArt**
- Đăng bài, trả lời, **like** bài viết và bình luận
- Lọc bài theo **Mới nhất / Thịnh hành / Danh mục**
- Hiển thị avatar người dùng trong bài đăng và bình luận
- Phân trang và đếm lượt xem, lượt trả lời

### 📰 Tin tức
- Bảng tin VTuber mới nhất với ảnh thumbnail
- Phân loại theo: Event, Debut, Music, ASMR, Gaming

### 👤 Tài khoản người dùng
- Đăng ký, đăng nhập, đăng xuất với xác thực Cookie
- Mật khẩu mã hóa bằng **SHA-256 + salt**
- Upload và cập nhật ảnh đại diện qua **Cloudinary**
- Phân quyền **Admin / Editor / User**
- Trang quản trị dành riêng cho Admin

### 🌍 Đa ngôn ngữ (i18n)
- Hỗ trợ 3 ngôn ngữ: **Tiếng Anh (en)**, **Tiếng Việt (vi)**, **Tiếng Nhật (ja)**
- Chuyển đổi ngôn ngữ trực tiếp trên giao diện
- Tất cả nhãn, thông báo lỗi, nội dung UI đều được bản địa hóa đầy đủ

### 📊 Activity Feed
- Theo dõi hoạt động cộng đồng trong thời gian thực (tạo bài, chỉnh sửa, bình luận)

---

## 🛠️ Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| **Framework** | ASP.NET Core MVC (.NET 10) |
| **Database** | Microsoft SQL Server + Entity Framework Core 10 |
| **ORM / Migration** | EF Core Code-First Migrations |
| **Lưu trữ ảnh** | Cloudinary (CDN) |
| **Xác thực** | Cookie Authentication (ASP.NET Core Identity-style) |
| **Đa ngôn ngữ** | ASP.NET Core Localization (`.resx` resource files) |
| **Frontend** | Razor Views (`.cshtml`), HTML/CSS/JavaScript |
| **Hosting DB** | Somee.com (SQL Server) |

---

## 📁 Cấu trúc dự án

```
Webcuoiky/
├── WebWikiForum/
│   ├── Controllers/          # Xử lý logic HTTP
│   │   ├── AccountController.cs
│   │   ├── AdminController.cs
│   │   ├── ForumController.cs
│   │   ├── WikiController.cs
│   │   ├── HomeController.cs
│   │   └── LanguageController.cs
│   ├── Models/               # Entity models (EF Core)
│   │   ├── Vtuber.cs
│   │   ├── Agency.cs
│   │   ├── Discussion.cs
│   │   ├── User.cs
│   │   ├── News.cs
│   │   └── Activity.cs
│   ├── Views/                # Razor Views (UI)
│   │   ├── Wiki/             # Trang wiki VTuber & agency
│   │   ├── Forum/            # Diễn đàn cộng đồng
│   │   ├── Account/          # Đăng nhập / đăng ký
│   │   ├── Admin/            # Trang quản trị
│   │   ├── Home/             # Trang chủ & tin tức
│   │   └── Shared/           # Layout & partial views
│   ├── Services/             # Business logic
│   │   ├── FileService.cs    # Upload ảnh Cloudinary
│   │   └── ActivityService.cs
│   ├── Resources/            # File bản địa hóa (.resx)
│   │   ├── SharedResource.en.resx
│   │   ├── SharedResource.vi.resx
│   │   └── SharedResource.ja.resx
│   ├── Data/                 # ApplicationDbContext
│   ├── Migrations/           # EF Core migrations
│   ├── ViewModels/           # ViewModel cho các View
│   ├── Program.cs            # Cấu hình ứng dụng & seed data
│   └── appsettings.json      # Chuỗi kết nối & config
└── Webcuoiky.sln
```

---

## ⚙️ Hướng dẫn cài đặt

### Yêu cầu hệ thống
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB hoặc full)
- Tài khoản [Cloudinary](https://cloudinary.com/) (miễn phí)

### Các bước cài đặt

**1. Clone repository**
```bash
git clone <repository-url>
cd Webcuoiky
```

**2. Cấu hình `appsettings.json`**

Chỉnh sửa file `WebWikiForum/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VTWikiDB;Trusted_Connection=True;"
  },
  "Cloudinary": {
    "CloudName": "your_cloud_name",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret"
  }
}
```

**3. Chạy database migration**
```bash
cd WebWikiForum
dotnet ef database update
```

**4. Khởi chạy ứng dụng**
```bash
dotnet run
```

Truy cập: `https://localhost:5001`

> **Lưu ý:** Ứng dụng sẽ tự động seed dữ liệu mẫu (agencies, VTubers, tin tức) và tạo tài khoản Admin khi chạy lần đầu.

---
## 🗃️ Database Schema

Các bảng chính trong cơ sở dữ liệu:

| Bảng | Mô tả |
|---|---|
| `Users` | Tài khoản người dùng (Username, Email, PasswordHash, Role, AvatarUrl) |
| `Vtubers` | Thông tin VTuber (tên, debut, agency, trạng thái duyệt,...) |
| `Agencies` | Thông tin các agency VTuber |
| `Discussions` | Bài đăng diễn đàn |
| `DiscussionReplies` | Bình luận/trả lời trong diễn đàn |
| `DiscussionLikes` | Lượt thích bài đăng |
| `News` | Tin tức VTuber |
| `Activities` | Nhật ký hoạt động cộng đồng |

---

## 🤝 Nhóm phát triển

Dự án được phát triển bởi nhóm sinh viên trong khuôn khổ đồ án cuối kỳ.

- **Yoshino** — Trưởng nhóm & Backend Developer
- **Loc123** — Frontend & UI/UX
- **QuocAnh** — Database & Integration

---

## 📄 Giấy phép

Dự án này được phát triển cho mục đích học thuật. Mọi nội dung liên quan đến VTuber thuộc quyền sở hữu của các agency và cá nhân tương ứng.
