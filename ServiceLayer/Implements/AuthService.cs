using BusinessObject.Dtos.RequestModel;
using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using ServiceLayer.Interfaces;
using BusinessObject.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObject.Enums;

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

        public async Task<(bool Success, string Message)> VerifyOtpAndCreateAccountAsync(VerifyOtpRequest request, string roleName)
        {
            // Validate OTP same as VerifyOtpAndLoginAsync
            var isExist = _cache.TryGetValue($"OTP_{request.Email}", out string savedOtp);

            if (!isExist)
                return (false, "Mã OTP đã hết hạn hoặc không tồn tại.");

            if (savedOtp != request.OtpCode)
                return (false, "Mã OTP không chính xác.");

            // Remove OTP
            _cache.Remove($"OTP_{request.Email}");

            // Create account with provided password
            var existingUserInfo = await _accountRepository.GetUserInfoByEmailAsync(request.Email);
            if (existingUserInfo != null)
                return (false, "Email đã tồn tại trong hệ thống.");
            
            var roleEnum = BusinessObject.Enums.RoleEnum.Lecture;
            if (!string.IsNullOrEmpty(roleName) && Enum.TryParse<BusinessObject.Enums.RoleEnum>(roleName, true, out var parsedRole))
            {
                roleEnum = parsedRole;
            }
            var account = new Account
            {
                Account_id = Guid.NewGuid(),
                Username = string.IsNullOrEmpty(request.Username) ? request.Email : request.Username,
                Password = string.IsNullOrEmpty(request.Password) ? Guid.NewGuid().ToString("N") + "@A1" : request.Password,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                LastLogin = DateTime.UtcNow,
                Role = roleEnum
            };

            // Map roleName to RoleEnum if needed; here assume Teacher
            // Create user information
            var userInfo = new BusinessObject.Entities.UserInformation
            {
                User_id = Guid.NewGuid(),
                Account_id = account.Account_id,
                Email = request.Email,
                Name = account.Username
            };

            await _accountRepository.CreateAccountWithUserInfoAsync(account, userInfo);

            // Email credentials to the teacher
            var subject = "Thông tin tài khoản giảng viên";
            var body = $@"<div>
                <p>Xin chào,</p>
                <p>Tài khoản giảng viên đã được tạo</p>
                <p>Email: {request.Email}</p>
                <p>Password: {account.Password}</p>
                <p>Vui lòng đổi mật khẩu sau khi đăng nhập.</p>
            </div>";

            await _emailService.SendEmailAsync(request.Email, subject, body);

            return (true, "Tạo tài khoản thành công và gửi email thông báo.");
        }

        public async Task<(bool Success, string Message, UserDto? User, bool RequireOtp)> LoginAsync(string email, string password)
        {
            // 1. Tìm thông tin User và Account dựa vào Email
            var userInfo = await _accountRepository.GetUserInfoByEmailAsync(email);

            if (userInfo == null)
            {
                return (false, "Email hoặc mật khẩu không chính xác.", null, false);
            }

            var account = userInfo.Account;

            // 2. Kiểm tra trạng thái tài khoản
            if (!account.IsActive)
            {
                return (false, "Tài khoản của bạn đã bị khóa.", null, false);
            }

            // 3. Kiểm tra Mật khẩu (So sánh trực tiếp, sau này bạn dùng BCrypt/Identity thì thay đổi chỗ này)
            if (account.Password != password)
            {
                return (false, "Email hoặc mật khẩu không chính xác.", null, false);
            }

            // 4. Đúng mật khẩu -> Chuẩn bị data trả về cho Controller
            var userDto = new UserDto
            {
                Id = account.Account_id,
                Email = userInfo.Email,
                FullName = userInfo.Name,
                Role = account.Role.ToString() ?? "Customer" // RoleEnum -> string
            };

            // Include LastLogin
            userDto.LastLogin = account.LastLogin;

            // Determine whether OTP is required: if never logged in or last login more than 1 day ago
            var requireOtp = !account.LastLogin.HasValue || (DateTime.UtcNow - account.LastLogin.Value) > TimeSpan.FromDays(1);

            if (!requireOtp)
            {
                // Update last login immediately
                account.LastLogin = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(account);
                userDto.LastLogin = account.LastLogin;
            }

            return (true, "Xác thực mật khẩu thành công.", userDto, requireOtp);
        }

        public async Task<string> RequestOtpAsync(string email)
        {
            // Sinh mã OTP
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu vào Cache (5 phút)
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set($"OTP_{email}", otpCode, cacheOptions);

            // 3. GỌI DỊCH VỤ GỬI EMAIL THẬT TẠI ĐÂY
            var subject = "Mã xác thực đăng nhập Chat Bot App";

            // Bạn có thể thiết kế nội dung bằng HTML cho đẹp mắt
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xin chào!</h2>
                    <p>Bạn vừa yêu cầu mã xác thực để đăng nhập vào Chat Bot App.</p>
                    <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #2e6c80;'>{otpCode}</strong></p>
                    <p><i>Mã này sẽ hết hạn trong 5 phút. Vui lòng không chia sẻ cho người khác.</i></p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, htmlBody);

            return "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
        }


        public async Task<AuthenticationResultDto> VerifyOtpAndLoginAsync(VerifyOtpRequest request)
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

                    Username = string.IsNullOrEmpty(request.Username) ? request.Email : request.Username,
                    // Use provided password from registration flow if available; otherwise generate a random one
                    Password = string.IsNullOrEmpty(request.Password) ? Guid.NewGuid().ToString("N") + "@A1" : request.Password,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Role = BusinessObject.Enums.RoleEnum.Lecture
                };

                var userInfo = new UserInformation
                {
                    User_id = Guid.NewGuid(),
                    Account_id = account.Account_id,
                    Email = request.Email,
                    Name = account.Username
                };

                await _accountRepository.CreateAccountWithUserInfoAsync(account, userInfo);
                // Set LastLogin when creating a new account
                account.LastLogin = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(account);
            }
            else
            {
                account = existingUserInfo.Account;
                if (!account.IsActive) throw new Exception("Tài khoản đã bị khóa.");

                account.LastLogin = DateTime.UtcNow;
                await _accountRepository.UpdateAccountAsync(account);
            }

            // Trả về thông tin (Sau này thay bằng hàm Generate JWT Token)
            return new AuthenticationResultDto
            {
                AccountId = account.Account_id,
                Email = request.Email,
                Name = existingUserInfo?.Name,
                Role = account.Role,
                Message = "Đăng nhập thành công!"
            };
        }
    }
}
