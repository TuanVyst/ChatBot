using BusinessObject.Entities;

namespace DataAccessLayer.Repositories
{
    public class UserRepository : IUserRepository
    {
        public Task<User?> GetByEmailAsync(string email)
        {
            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    FullName = "Student Demo",
                    Email = "student@gmail.com",
                    PasswordHash = "123456",
                    Role = "Student"
                },
                new User
                {
                    Id = 2,
                    FullName = "Teacher Demo",
                    Email = "teacher@gmail.com",
                    PasswordHash = "123456",
                    Role = "Teacher"
                }
            };

            var user = users.FirstOrDefault(x =>
                x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(user);
        }
    }
}