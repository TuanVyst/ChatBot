namespace ChatBot.Models
{
    public class VerifyOtpViewModel
    {
        public string Email { get; set; } // Sẽ được ẩn đi (Hidden field) trên giao diện
        public string OtpCode { get; set; }
    }
}
