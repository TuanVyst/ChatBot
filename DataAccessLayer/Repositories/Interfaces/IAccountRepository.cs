using BusinessObject.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<UserInformation> GetUserInfoByEmailAsync(string email);

        Task CreateAccountWithUserInfoAsync(Account account, UserInformation userInfo);
        Task UpdateAccountAsync(Account account);
    }
}
