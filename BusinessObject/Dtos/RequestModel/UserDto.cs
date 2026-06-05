namespace BusinessObject.Dtos.RequestModel
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
