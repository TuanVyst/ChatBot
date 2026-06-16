using BusinessObject.Dtos.RequestModel;
using BusinessObject.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IAuthService
    {
        Task<string> RequestOtpAsync(string email);
        Task<AuthenticationResultDto> VerifyOtpAndLoginAsync(VerifyOtpRequest request);
        Task<(bool Success, string Message)> VerifyOtpAndCreateAccountAsync(VerifyOtpRequest request, string roleName);
        Task<(bool Success, string Message, UserDto? User, bool RequireOtp)> LoginAsync(
        string email,
        string password);
        Task<bool> ValidateAccountAsync(string email);
    }
}
