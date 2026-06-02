namespace BusinessObject.Dtos
{
    public class AuthenticationResultDto
    {
        public System.Guid AccountId { get; set; }
        public string Email { get; set; }
        public string? Name { get; set; }
        public string Message { get; set; }
    }
}
