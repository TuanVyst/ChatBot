using BusinessObject.Enums;

namespace BusinessObject.Dtos.RequestModel
{
    public class AuthenticationResultDto
    {
        public System.Guid AccountId { get; set; }
        public string Email { get; set; }
        public string? Name { get; set; }
        public RoleEnum Role { get; set; }
        public string Message { get; set; }
    }
}
