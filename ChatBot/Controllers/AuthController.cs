using BusinessObject.Dtos.RequestModel;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interfaces;

namespace ChatBot.Controllers
{
   
        [Route("api/[controller]")]
        [ApiController]
        public class AuthController : Controller
        {

            private readonly IAuthService _authService;

            // Inject Service vào Controller
            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }

            [HttpPost("request-otp")]
            public async Task<IActionResult> RequestOtp([FromBody] RequestOtp request)
            {
                try
                {
                    var result = await _authService.RequestOtpAsync(request.Email);
                    return Ok(new { Message = result });
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            [HttpPost("verify-otp-login")]
            public async Task<IActionResult> VerifyOtpLogin([FromBody] VerifyOtpRequest request)
            {
                try
                {
                    var result = await _authService.VerifyOtpAndLoginAsync(request);
                    return Ok(new { Data = result });
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }
    }

