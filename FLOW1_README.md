## Flow 1: Quản lý tài liệu - Hướng dẫn sử dụng

### Mô tả
Flow 1 cho phép upload tài liệu (PDF, DOCX, PPT, PPTX) và tự động indexing chúng. Hệ thống sẽ:
1. Trích xuất text từ tài liệu
2. Chia text thành chunks
3. Tạo embedding cho mỗi chunk sử dụng OpenAI API
4. Lưu vào cơ sở dữ liệu PostgreSQL

### Yêu cầu
- PostgreSQL đang chạy
- OpenAI API key (từ https://platform.openai.com/api-keys)
- .NET 8.0 SDK
- Upload folder: `D:\Upload` (hoặc thay đổi trong `.env`)

### Cấu hình

#### 1. Setup .env file
```
OPENAI_API_KEY=sk-your-actual-key-here
UploadFolderPath=D:\Upload
MaxFileSize=314572800   # 300MB
ChunkSize=512          # ký tự
```

#### 2. Database setup
```bash
cd D:\GitHub\PRN222_ChatBot
dotnet ef database update --project DataAccessLayer --startup-project ChatBot
```

#### 3. Chạy ứng dụng
```bash
cd ChatBot
dotnet run
```

### API Endpoints

#### GET /api/subject
Lấy danh sách môn học (hardcode)
```
Response: ["Lập trình C#", "Cấu trúc dữ liệu", ...]
```

#### POST /api/document/upload
Upload tài liệu và tự động index
```
Query params:
  - subjectName: "Lập trình C#" (bắt buộc)
  - chapterName: "Chương 1" (tùy chọn)

Form Data:
  - file: [file] (PDF, DOCX, PPT, PPTX)

Response:
{
  "documentId": 1,
  "message": "File uploaded and indexed successfully"
}
```

#### GET /api/document
Lấy danh sách tài liệu đã index hoàn thành
```
Query params:
  - subjectName: "Lập trình C#" (tùy chọn, để trống lấy tất cả)

Response:
[
  {
    "id": 1,
    "fileName": "Lecture1.pdf",
    "subjectName": "Lập trình C#",
    "chapterName": "Chương 1",
    "indexStatus": "Completed",
    "uploadDate": "2026-05-26T10:00:00",
    "fileSize": 5242880
  }
]
```

#### POST /api/document/{id}/reindex
Index lại một tài liệu (xóa chunks cũ rồi index mới)
```
Response: "Document reindexed successfully"
```

### Luồng xử lý

1. **Upload**: File được lưu vào `D:\Upload\` với tên unique
2. **Extract**: Text được trích xuất từ file (theo định dạng)
3. **Chunking**: Text được chia thành 512 ký tự/chunk
4. **Embedding**: Mỗi chunk được convert thành vector 1536 chiều via OpenAI
5. **Index**: Vector + text được lưu vào bảng `DocumentChunks`

### Rollback & Error Handling

- Nếu lỗi ở bất kỳ bước nào → **Rollback tất cả changes DB** + **Xóa file local**
- Trạng thái được lưu: `Pending` → `Processing` → `Completed` / `Failed`
- Lỗi được lưu ở `Document.ErrorMessage` để debug

### Hạn chế hiện tại

- Chỉ hỗ trợ PDF, DOCX, PPT, PPTX
- Không xử lý ảnh
- Xử lý tuần tự (một file tại một thời điểm)
- Maksimum file size: 300MB

### Ví dụ cURL

```bash
# Upload file
curl -X POST http://localhost:5000/api/document/upload \
  -F "file=@lecture.pdf" \
  -F "subjectName=Lập trình C#" \
  -F "chapterName=Chương 1"

# List documents
curl http://localhost:5000/api/document?subjectName=Lập%20trình%20C%23

# Reindex
curl -X POST http://localhost:5000/api/document/1/reindex
```

