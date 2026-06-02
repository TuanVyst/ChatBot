using BusinessObject.Entities;
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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Kích hoạt extension pgvector trong PostgreSQL
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<DocumentChunk>()
            .Property(e => e.Embedding)
            .HasColumnType("vector(3072)");
        }
    }
}