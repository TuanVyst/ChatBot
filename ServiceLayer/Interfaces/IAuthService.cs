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
        Task<BusinessObject.Dtos.AuthenticationResultDto> VerifyOtpAndLoginAsync(VerifyOtpRequest request);
        Task<(bool Success, string Message, BusinessObject.Dtos.UserDto? User, bool RequireOtp)> LoginAsync(
        string email,
        string password);
    }
}
