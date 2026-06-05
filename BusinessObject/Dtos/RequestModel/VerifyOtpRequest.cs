using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Dtos.RequestModel
{
    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
        // Optional password used when verifying OTP for registration flow
        public string Password { get; set; }
        // Optional username used when registering a new account
        public string Username { get; set; }
    }
    public class RequestOtp
    {
        public string Email { get; set; }
    }
}
