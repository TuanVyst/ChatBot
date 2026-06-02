using BusinessObject.Entities;

namespace ServiceLayer.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User)> LoginAsync(
            string email,
            string password);
    }
}