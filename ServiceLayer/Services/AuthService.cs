using BusinessObject.Entities;
using DataAccessLayer.Repositories;

namespace ServiceLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(
            string email,
            string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email không được để trống.", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Mật khẩu không được để trống.", null);

            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
                return (false, "Tài khoản không tồn tại.", null);

            if (user.PasswordHash != password)
                return (false, "Sai mật khẩu.", null);

            return (true, "Đăng nhập thành công.", user);
        }
    }
}