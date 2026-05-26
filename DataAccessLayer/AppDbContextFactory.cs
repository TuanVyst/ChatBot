using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


namespace DataAccessLayer
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Cung cấp một chuỗi kết nối (Connection string)
            // Lưu ý: Chuỗi này CHỈ dùng để EF Core có thể tạo file Migration. 
            // Khi chạy ứng dụng thực tế, nó vẫn sẽ dùng chuỗi kết nối trong appsettings.json của ChatBot.
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ChatBotDb;Username=postgres;Password=12345678");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
