using BusinessObject.Dtos.RequestModel;
using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService; // 1. Khai báo thêm EmailService

        // 2. Inject vào Constructor
        public AuthService(IAccountRepository accountRepository, IMemoryCache cache, IEmailService emailService)
        {
            _accountRepository = accountRepository;
            _cache = cache;
            _emailService = emailService;
        }

        public async Task<string> RequestOtpAsync(string email)
        {
            // Sinh mã OTP
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu vào Cache (5 phút)
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set($"OTP_{email}", otpCode, cacheOptions);

            // 3. GỌI DỊCH VỤ GỬI EMAIL THẬT TẠI ĐÂY
            var subject = "Mã xác thực đăng nhập SmartMeal";

            // Bạn có thể thiết kế nội dung bằng HTML cho đẹp mắt
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xin chào!</h2>
                    <p>Bạn vừa yêu cầu mã xác thực để đăng nhập vào SmartMeal.</p>
                    <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #2e6c80;'>{otpCode}</strong></p>
                    <p><i>Mã này sẽ hết hạn trong 5 phút. Vui lòng không chia sẻ cho người khác.</i></p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, htmlBody);

            return "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
        }


        public async Task<object> VerifyOtpAndLoginAsync(VerifyOtpRequest request)
        {
            // 1. Lấy OTP từ Cache ra để kiểm tra
            var isExist = _cache.TryGetValue($"OTP_{request.Email}", out string savedOtp);

            if (!isExist)
                throw new Exception("Mã OTP đã hết hạn hoặc không tồn tại.");

            if (savedOtp != request.OtpCode)
                throw new Exception("Mã OTP không chính xác.");

            // 2. OTP đúng -> Xóa OTP khỏi cache để tránh dùng lại (Replay Attack)
            _cache.Remove($"OTP_{request.Email}");

            // 3. Tiến hành Đăng nhập (hoặc Đăng ký nếu chưa có tài khoản)
            var existingUserInfo = await _accountRepository.GetUserInfoByEmailAsync(request.Email);
            Account account;

            if (existingUserInfo == null)
            {

                account = new Account
                {
                    Account_id = Guid.NewGuid(),

                    Username = request.Email,
                    Password = Guid.NewGuid().ToString("N") + "@A1", // Pass ngẫu nhiên
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var userInfo = new UserInformation
                {
                    User_id = Guid.NewGuid(),
                    Account_id = account.Account_id,
                    Email = request.Email,
                    Name = request.Email.Split('@')[0]
                };

                await _accountRepository.CreateAccountWithUserInfoAsync(account, userInfo);
            }
            else
            {
                account = existingUserInfo.Account;
                if (!account.IsActive) throw new Exception("Tài khoản đã bị khóa.");

                account.LastLogin = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(account);
            }

            // Trả về thông tin (Sau này thay bằng hàm Generate JWT Token)
            return new
            {
                AccountId = account.Account_id,
                Email = request.Email,
                Message = "Đăng nhập thành công!"
            };
        }
    }
}
