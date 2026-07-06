using DataAccessLayer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Repositories.Implements;
using ServiceLayer.Implements;
using ServiceLayer.Interfaces;
using DataAccessLayer.Repositories;
using BusinessObject.Entities;
using BCrypt.Net;


DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"));


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector())); 

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentChunkService, DocumentChunkService>();

// Register custom services
var uploadFolderPath = builder.Configuration["UploadFolderPath"] ?? "D:\\Upload";


var maxFileSize = long.TryParse(builder.Configuration["MaxFileSize"], out var size) ? size : 3145728; // 3MB default

var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var chunkSize = int.TryParse(builder.Configuration["ChunkSize"], out var cs) ? cs : 512;

builder.Services.AddSingleton<IFileUploadService>(new FileUploadService(uploadFolderPath, maxFileSize));
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();
builder.Services.AddScoped<IChunkingService>(sp => new ChunkingService(chunkSize, 50));
builder.Services.AddScoped<IEmbeddingService>(sp => new EmbeddingService(geminiApiKey ?? throw new InvalidOperationException("GEMINI_API_KEY or OPENAI_API_KEY not configured")));
builder.Services.AddScoped<IChatService>(sp => new ChatService(geminiApiKey ?? throw new InvalidOperationException("GEMINI_API_KEY or OPENAI_API_KEY not configured")));
builder.Services.AddScoped<IIndexingService, IndexingService>();
builder.Services.AddScoped<IRetrievalService, RetrievalService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddScoped<IRagService, RagService>();


builder.Services.AddScoped<IUniversityRepository, UniversityRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
builder.Services.AddScoped<IUniversityService, UniversityService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IChapterService, ChapterService>();



builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "ChatBot.Auth";
        options.ForwardDefaultSelector = ctx =>
        {
            var path = ctx.Request.Path.Value ?? "";
            if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "AdminScheme";
            }
            if (path.StartsWith("/Lecturer", StringComparison.OrdinalIgnoreCase))
            {
                return "LectureScheme";
            }
            if (path.StartsWith("/Student", StringComparison.OrdinalIgnoreCase))
            {
                return "StudentScheme";
            }

            var referer = ctx.Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                try
                {
                    var refererUri = new Uri(referer);
                    var refererPath = refererUri.AbsolutePath;
                    if (refererPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return "AdminScheme";
                    }
                    if (refererPath.StartsWith("/Lecturer", StringComparison.OrdinalIgnoreCase))
                    {
                        return "LectureScheme";
                    }
                    if (refererPath.StartsWith("/Student", StringComparison.OrdinalIgnoreCase))
                    {
                        return "StudentScheme";
                    }
                }
                catch
                {
                    // Ignore malformed referer headers
                }
            }

            return null;
        };
    })
    .AddCookie("AdminScheme", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "ChatBot.Auth.Admin";
    })
    .AddCookie("LectureScheme", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "ChatBot.Auth.Lecture";
    })
    .AddCookie("StudentScheme", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Cookie.Name = "ChatBot.Auth.Student";
    });

builder.Services.AddAuthorization();

builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddSignalR();

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
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Chatbot API v1");
            c.RoutePrefix = "swagger";
        });
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapFallbackToPage("/Auth/Login");
app.MapHub<ChatBot.Hubs.NotificationHub>("/notificationHub");

SeedDatabase(app);

app.Run();

void SeedDatabase(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var accountRepository = services.GetRequiredService<IAccountRepository>();

            // Check if admin user exists
            var adminUser = context.UserInformations.FirstOrDefault(u => u.Email == "chickenhuy2005@gmail.com");
            if (adminUser == null)
            {
                // Create admin account
                var adminAccount = new Account
                {
                    Username = "admin",
                    Password = global::BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = BusinessObject.Enums.RoleEnum.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var adminInfo = new UserInformation
                {
                    Account_id = adminAccount.Account_id,
                    Email = "chickenhuy2005@gmail.com",
                    Name = "Admin"
                };

                accountRepository.CreateAccountWithUserInfoAsync(adminAccount, adminInfo).Wait();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
