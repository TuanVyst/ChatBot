# PRN222_ChatBot (RAG Chatbot for University)

Dự án Hệ thống Chatbot thông minh tích hợp RAG (Retrieval-Augmented Generation) dành cho Trường Đại học. Hệ thống cho phép Giảng viên tải tài liệu bài giảng lên, tự động xử lý (chunking, embedding) và lưu trữ vector để Sinh viên có thể đặt câu hỏi và nhận câu trả lời từ AI dựa trên kiến thức của tài liệu môn học.

## 🚀 Công nghệ sử dụng
- **Framework:** ASP.NET Core MVC 8.0
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core
- **Vector Database:** PostgreSQL với extension `pgvector`
- **AI Integration:** OpenAI API (dành cho Embedding tài liệu và Text Generation)

## 🎯 Các tính năng chính
- **Admin:** Quản lý User (Giảng viên, Sinh viên), Quản lý trường học (University) và Môn học (Subject).
- **Giảng viên (Lecturer):** 
  - Tạo và quản lý các Chương (Chapters) cho môn học.
  - Upload tài liệu (PDF, DOCX, TXT), hệ thống sẽ tự động băm nhỏ (chunking) và index vector dưới nền (background processing).
  - Quản lý danh sách sinh viên tham gia khóa học.
  - Theo dõi tiến trình xử lý tài liệu với Progress Bar trực quan.
- **Sinh viên (Student):** 
  - Đăng nhập và chọn môn học đã được giảng viên thêm vào.
  - Đặt câu hỏi cho Chatbot dựa trên kiến thức từ các tài liệu đã được giảng viên cung cấp (Sử dụng RAG).

## 🛠 Yêu cầu hệ thống
- .NET 8.0 SDK
- PostgreSQL (phiên bản 15+ khuyến nghị)
- Bắt buộc phải cài đặt [pgvector extension](https://github.com/pgvector/pgvector) trên PostgreSQL.

## ⚙️ Cài đặt và Chạy dự án

### 1. Cấu hình biến môi trường
Dự án sử dụng file `.env` ở thư mục gốc (cùng cấp với file solution `.sln`) và `appsettings.json` trong thư mục `ChatBot`.

Tạo file `.env` với nội dung sau:
```env
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USER=your_email@gmail.com
EMAIL_PASS=your_app_password

# Thay bằng OpenAI API Key thực tế của bạn
OPENAI_API_KEY=sk-...

UploadFolderPath=D:\Upload
MaxFileSize=314572800
ChunkSize=512
```

Cấu hình chuỗi kết nối Database trong file `ChatBot/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=PRN222_ChatBot;Username=postgres;Password=matkhau_cuaban"
  }
}
```

### 2. Cập nhật Database (Entity Framework Core)
Mở Terminal tại thư mục gốc của dự án và chạy các lệnh sau để tạo schema cho database:
```bash
dotnet ef database update --project DataAccessLayer --startup-project ChatBot
```
*Lưu ý: Nếu bạn gặp lỗi schema bị lệch, hãy chạy `dotnet ef database drop --force` trước khi update lại (chỉ dành cho môi trường dev).*

### 3. Chạy dự án
Chạy lệnh sau để khởi động ứng dụng Web:
```bash
dotnet run --project ChatBot
```
Hệ thống sẽ tự động Seed (tạo) một tài khoản Admin mặc định khi chạy lần đầu:
- **Username:** admin
- **Password:** 123456
- **Email:** chickenhuy2005@gmail.com

---
*Dự án PRN222 - Được phát triển bằng ASP.NET Core MVC.*