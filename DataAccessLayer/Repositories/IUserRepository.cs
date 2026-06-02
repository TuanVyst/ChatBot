using BusinessObject.Entities;

namespace DataAccessLayer.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
    }
}