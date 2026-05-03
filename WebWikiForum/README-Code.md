# 📚 VT-Wiki Project Documentation

Chào mừng bạn đến với tài liệu hướng dẫn kỹ thuật của dự án **VT-Wiki & Forum**. Đây là bản tóm tắt các chức năng cốt lõi và vị trí của chúng trong mã nguồn để bạn dễ dàng bảo trì và phát triển.

---

## 🛠️ 1. Các Chức Năng Cốt Lõi (Core Features)

### 👑 Quản trị hệ thống (Admin Dashboard)
*   **Vị trí:** `Controllers/AdminController.cs` | `Views/Admin/Dashboard.cshtml`
*   **Chức năng:** Thống kê hệ thống, quản lý người dùng, phân quyền Editor.
*   **Điểm nhấn:** Sử dụng **Alpine.js** để xử lý các Modal phân quyền mượt mà không cần load lại trang.

### 🛡️ Phân quyền & Giao việc (RBAC & Assignment)
*   **Vị trí:** `Models/EditorAssignment.cs` | `Controllers/AdminController.cs` (Action `AssignEditor`, `RemoveEditorAssignment`)
*   **Chức năng:** Admin có quyền giao cho Editor quản lý một bài viết cụ thể hoặc tước quyền quản lý đó.
*   **Logic:** Kiểm tra quyền trong `AdminController` và hiển thị danh sách bài viết được giao trong Dashboard của Editor.

### 🔔 Hệ thống Thông báo (Notification System)
*   **Vị trí:** 
    *   **Backend:** `ViewComponents/NavbarProfileViewComponent.cs` (Logic lọc thông báo)
    *   **Service:** `Services/ActivityService.cs` (Ghi đè lịch sử hoạt động)
    *   **Frontend:** `Views/Shared/Components/NavbarProfile/Default.cshtml` (Giao diện chuông thông báo)
*   **Logic:** 
    *   **Admin:** Thấy tất cả hoạt động (bao gồm cả lượt Like).
    *   **User:** Thấy các hoạt động hệ thống và chỉ thấy bình luận ở các bài viết mình có tham gia.

### 🌐 Đa ngôn ngữ (Localization)
*   **Vị trí:** `Resources/` | `Controllers/LanguageController.cs`
*   **Chức năng:** Chuyển đổi giữa Tiếng Anh, Tiếng Việt và Tiếng Nhật. Dữ liệu được lưu trong các file `.resx`.

### 💬 Diễn đàn & Tương tác (Forum & Interaction)
*   **Vị trí:** `Controllers/ForumController.cs` | `Views/Forum/`
*   **Chức năng:** Đăng bài, bình luận, và hệ thống **Toggle Like** (Thích bài viết/bình luận).
*   **Logic Like:** Xử lý bằng AJAX tại `ToggleLikeDiscussion` và `ToggleLikeReply`.

### 🤖 Trợ lý ảo Yoshi (Chatbot AI)
*   **Vị trí:**
    *   **Backend:** `Controllers/ChatController.cs` (Xử lý logic Ask, Poll, History)
    *   **Model:** `Models/ChatMessage.cs` (Lưu trữ lịch sử hội thoại)
    *   **Frontend:** `wwwroot/js/chat-widget.js` & `wwwroot/css/chat-widget.css`
*   **Chức năng:** Trò chuyện trực tuyến với AI (Yoshi), hỗ trợ giải đáp thắc mắc về Vtubers và hệ thống.
*   **Công nghệ:** Tích hợp bộ lọc tin nhắn, lưu trữ session chat trong LocalStorage và Polling để nhận tin nhắn từ Server.

---

## 📁 2. Cấu trúc thư mục (Project Structure)

```text
WebWikiForum/
├── Controllers/         # Điều hướng và xử lý logic nghiệp vụ chính
├── Models/              # Định nghĩa cấu trúc dữ liệu (Database Entities)
├── ViewModels/          # Dữ liệu trung gian truyền từ Controller ra View
├── Data/                # Cấu hình Entity Framework và Migrations
├── Services/            # Các dịch vụ dùng chung (Upload ảnh, Log hoạt động)
├── ViewComponents/      # Các thành phần giao diện động (Menu, Thông báo)
├── Views/               # Giao diện Razor HTML (Tailwind CSS)
│   ├── Admin/           # Dashboard quản trị
│   ├── Shared/          # Các thành phần dùng chung (Layout, Navbar)
│   └── ...              # Các Views theo từng Controller
└── wwwroot/             # Tài nguyên tĩnh (CSS, JS, Hình ảnh)
```

---

## 🚀 3. Quy trình Phát triển (Workflow)

1.  **Database:** Nếu thay đổi Model, hãy chạy Migration:
    ```powershell
    dotnet ef migrations add <TenMigration>
    dotnet ef database update
    ```
2.  **Frontend:** Dự án sử dụng **Tailwind CSS** (via CDN/JIT) và **Alpine.js** cho các tương tác phía client.
3.  **Localization:** Khi thêm text mới, hãy cập nhật vào các file trong thư mục `Resources`.

---

*Tài liệu này được tạo tự động bởi Antigravity để hỗ trợ việc quản lý dự án hiệu quả hơn.*
