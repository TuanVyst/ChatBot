using BusinessObject.Dtos.RequestModel;
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
        Task<object> VerifyOtpAndLoginAsync(VerifyOtpRequest request);
    }
}
