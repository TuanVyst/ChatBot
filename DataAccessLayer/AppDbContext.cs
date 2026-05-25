using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace DataAccessLayer
{
    public class AppDbContext : DbContext
    {


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Kích hoạt extension pgvector trong PostgreSQL
            modelBuilder.HasPostgresExtension("vector");
        }
    }
}