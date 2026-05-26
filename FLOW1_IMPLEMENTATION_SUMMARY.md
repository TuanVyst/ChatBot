# Flow 1 Implementation - Hoàn thành

## ✅ Những gì đã hoàn thành

### 1. **Entities & Database**
- ✅ Update `Document.cs` thêm: `ChapterName`, `IndexStatus`, `ErrorMessage`
- ✅ Tạo migration: `AddChapterAndIndexStatus`
- ✅ DocumentChunk entity giữ nguyên với `VectorJson` cho embedding

### 2. **Service Layer**
Tạo 5 services trong `ServiceLayer/Services/`:

- **FileUploadService.cs**
  - Upload file lên local `D:\Upload\` 
  - Validate định dạng (PDF, DOCX, PPT, PPTX)
  - Validate kích thước file (max 300MB)
  - Generate unique filename

- **TextExtractionService.cs**
  - Trích text từ PDF (iText7)
  - Trích text từ DOCX (DocumentFormat.OpenXml)
  - Trích text từ PPTX (slide title + content)
  - Trả về text hoặc error

- **ChunkingService.cs**
  - Chia text thành chunks (default 512 ký tự)
  - Hỗ trợ overlap (50 ký tự)
  - Xử lý Việt text

- **EmbeddingService.cs**
  - Gọi OpenAI API embedding (text-embedding-ada-002)
  - Trả về vector 1536 chiều
  - Error handling

- **IndexingService.cs**
  - Luồng chính: extract → chunk → embed → save
  - Transaction + Rollback on error
  - Xóa file local nếu lỗi
  - Update `Document.IndexStatus` (Pending → Processing → Completed/Failed)

### 3. **Controllers**
- **DocumentController.cs**
  - `POST /api/document/upload` - Upload + index tuần tự
  - `GET /api/document` - List tài liệu đã index
  - `POST /api/document/{id}/reindex` - Reindex tài liệu

- **SubjectController.cs**
  - `GET /api/subject` - Danh sách môn hardcode

### 4. **Configuration**
- ✅ Update `.env` với:
  - `OPENAI_API_KEY` - placeholder
  - `UploadFolderPath=D:\Upload`
  - `MaxFileSize=314572800` (300MB)
  - `ChunkSize=512`

- ✅ Update `appsettings.json` với upload config

- ✅ Update `Program.cs` đăng ký các services

### 5. **NuGet Packages**
- ✅ itext7 (PDF extraction)
- ✅ DocumentFormat.OpenXml (DOCX, PPTX)
- ✅ OpenAI (embedding API, v2.10.0)
- ✅ Pgvector (PostgreSQL vector support)

### 6. **Documentation**
- ✅ `FLOW1_README.md` - Hướng dẫn setup & API endpoints

---

## 🚀 Cách sử dụng

### 1. **Setup**
```bash
# Copy .env template, thêm OpenAI key
cp .env .env.local
# Edit .env.local:
# OPENAI_API_KEY=sk-your-actual-key-here

# Apply migration
dotnet ef database update --project DataAccessLayer --startup-project ChatBot
```

### 2. **Chạy**
```bash
cd ChatBot
dotnet run
```

Ứng dụng sẽ chạy trên `https://localhost:5001` (hoặc port khác)

### 3. **Test API**
```bash
# Lấy danh sách môn
curl https://localhost:5001/api/subject

# Upload file (PDF/DOCX/PPTX)
curl -X POST https://localhost:5001/api/document/upload \
  -F "file=@your-file.pdf" \
  -F "subjectName=Lập trình C#"

# Xem danh sách tài liệu đã index
curl https://localhost:5001/api/document?subjectName=Lập%20trình%20C%23
```

---

## 📋 Flow xử lý chi tiết

```
1. User upload file → 
2. FileUploadService lưu local → 
3. Document được tạo với status=Pending →
4. IndexingService.IndexDocumentAsync() chạy:
   a. ExtractText (PDF/DOCX/PPTX) →
   b. ChunkText (chia thành 512 ký tự) →
   c. Mỗi chunk gọi OpenAI embedding →
   d. Lưu DocumentChunk vào DB với vector
   e. Update Document.status = Completed
5. Nếu lỗi ở bất kỳ bước nào:
   - Rollback tất cả DB changes
   - Xóa file local
   - Update status = Failed + error message
```

---

## ⚠️ Lưu ý

### Bắt buộc setup trước chạy:
1. **PostgreSQL chạy**
2. **OpenAI API key** có sẵn
3. **Folder `D:\Upload`** tồn tại (hoặc config path khác)
4. **Database migration** đã apply

### Hạn chế hiện tại:
- Chỉ xử lý text (không ảnh)
- Xử lý tuần tự (1 file tại 1 thời điểm)
- Max file size 300MB
- Default chunk size 512 ký tự (cấu hình được)
- Hardcode 5 môn học

### OpenAI Cost:
- text-embedding-ada-002: ~$0.02 per 1M tokens
- Tính toán: File 300MB → ~30,000 pages PDF → ~10M tokens → ~$0.20/file

---

## 📁 File Structure

```
ServiceLayer/Services/
├── FileUploadService.cs
├── TextExtractionService.cs
├── ChunkingService.cs
├── EmbeddingService.cs
└── IndexingService.cs

ChatBot/Controllers/
├── DocumentController.cs
└── SubjectController.cs

DataAccessLayer/Migrations/
├── 20260525105739_InitialCreate.cs
└── 20260526000001_AddChapterAndIndexStatus.cs
```

---

## 🔍 Error Handling

Tất cả lỗi được log vào:
- `Document.ErrorMessage` (DB)
- HTTP response (API)

Ví dụ:
```json
{
  "documentId": 0,
  "message": "Indexing failed: Embedding failed: Rate limit exceeded"
}
```

Status document sẽ là `Failed`, có thể retry bằng reindex endpoint.

---

## ✨ Tính năng chính

✅ Upload PDF, DOCX, PPT, PPTX  
✅ Trích text tự động  
✅ Chia chunk thông minh (512 ký tự default)  
✅ Embedding via OpenAI (ada-002)  
✅ Index vào PostgreSQL  
✅ Rollback on error  
✅ Xóa file local on error  
✅ Track status (Pending/Processing/Completed/Failed)  
✅ Reindex capability  
✅ List by subject  

---

## 🎯 Flow 2, 3 sẽ xây dựng sau

- **Flow 2**: Hỏi đáp (RAG - Retrieval Augmented Generation)
- **Flow 3**: So sánh RAG vs Fine-tuning

