using BusinessObject.Entities;
using BusinessObject.Enums;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
namespace DataAccessLayer
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<UserInformation> UserInformations { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<StudentNotification> StudentNotifications { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<ChatHistorySource> ChatHistorySources { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Kích hoạt extension pgvector trong PostgreSQL
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<DocumentChunk>()
            .Property(e => e.Embedding)
            .HasColumnType("vector(3072)");

            modelBuilder.Entity<StudentNotification>()
                .HasIndex(n => new { n.AccountId, n.IsRead, n.CreatedAt });

            modelBuilder.Entity<ChatHistorySource>()
                .HasOne(chs => chs.ChatHistory)
                .WithMany(ch => ch.Sources)
                .HasForeignKey(chs => chs.ChatHistoryId);

            modelBuilder.Entity<ChatHistorySource>()
                .HasOne(chs => chs.DocumentChunk)
                .WithMany()
                .HasForeignKey(chs => chs.DocumentChunkId);

            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting
                {       
                    Id = 1,
                    ChunkSize = 512,
                    ChunkOverlap = 50,
                    TopK = 5,
                    EmbeddingModel = "text-embedding-3-small",
                    UpdatedAt = DateTime.UtcNow
                });

            // Subscription: unique index on OrderCode
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.OrderCode)
                .IsUnique();

            // Seed subscription plans
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Gói Tuần",
                    Price = 19000,
                    DurationDays = 7,
                    DailyQuestionLimit = 10,
                    Description = "Hỏi 10 câu/ngày trong 7 ngày",
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Gói Tháng",
                    Price = 49000,
                    DurationDays = 30,
                    DailyQuestionLimit = 10,
                    Description = "Hỏi 10 câu/ngày trong 30 ngày",
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Gói Năm",
                    Price = 490000,
                    DurationDays = 365,
                    DailyQuestionLimit = 10,
                    Description = "Hỏi 10 câu/ngày trong 365 ngày",
                    IsActive = true
                });
        }
    }
}
