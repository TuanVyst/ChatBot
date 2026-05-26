using DataAccessLayer;
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
