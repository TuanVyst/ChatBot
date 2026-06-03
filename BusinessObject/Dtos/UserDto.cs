namespace BusinessObject.Dtos
{
    public class UserDto
    {
        public System.Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public System.DateTime? LastLogin { get; set; }
    }
}
