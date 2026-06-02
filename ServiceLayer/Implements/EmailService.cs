using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class EmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Đọc thông tin cấu hình từ biến môi trường
            var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST");
            var emailPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL_PORT") ?? "587");
            var emailUser = Environment.GetEnvironmentVariable("EMAIL_USER");
            var emailPass = Environment.GetEnvironmentVariable("EMAIL_PASS");

            // Tạo nội dung bức thư
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SmartMeal App", emailUser));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            // Xây dựng body (hỗ trợ cả HTML)
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body, // Nếu bạn muốn gửi HTML, đổi TextBody thành HtmlBody
                TextBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Kết nối SMTP Server và gửi
            using var client = new SmtpClient();
            try
            {
                // Tùy chọn StartTls giúp mã hóa dữ liệu an toàn
                await client.ConnectAsync(emailHost, emailPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailUser, emailPass);

                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
