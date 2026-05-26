using DataAccessLayer;
using ServiceLayer.Services;
using Microsoft.EntityFrameworkCore;

// Load environment variables from .env file
try
{
    DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));
}
catch
{
    // .env file not found or error loading - use system environment variables
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register custom services
var uploadFolderPath = builder.Configuration["UploadFolderPath"] ?? "D:\\Upload";
var maxFileSize = long.TryParse(builder.Configuration["MaxFileSize"], out var size) ? size : 314572800; // 300MB default
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var chunkSize = int.TryParse(builder.Configuration["ChunkSize"], out var cs) ? cs : 512;

builder.Services.AddSingleton(new FileUploadService(uploadFolderPath, maxFileSize));
builder.Services.AddScoped<TextExtractionService>();
builder.Services.AddScoped(sp => new ChunkingService(chunkSize, 50));
builder.Services.AddScoped(sp => new EmbeddingService(openAiKey ?? throw new InvalidOperationException("OPENAI_API_KEY not configured")));
builder.Services.AddScoped<IndexingService>();

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RAG Chatbot API",
        Version = "v1",
        Description = "API cho hệ thống quản lý tài liệu và hỏi đáp AI"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            // Đường dẫn truy cập sẽ là: https://localhost:<port>/swagger
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Chatbot API v1");
            c.RoutePrefix = string.Empty;
        });
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
