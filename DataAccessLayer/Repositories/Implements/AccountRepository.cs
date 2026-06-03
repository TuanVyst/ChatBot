using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories.Implements
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserInformation> GetUserInfoByEmailAsync(string email)
        {
            return await _context.UserInformations
                .Include(u => u.Account)
                .FirstOrDefaultAsync(u => u.Email == email);
        }



        public async Task CreateAccountWithUserInfoAsync(Account account, UserInformation userInfo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Accounts.Add(account);
                _context.UserInformations.Add(userInfo);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAccountAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserInformation>> GetAllUserInformationsAsync()
        {
            return await _context.UserInformations.Include(u => u.Account).ToListAsync();
        }

        public async Task<Account> GetByIdAsync(Guid id)
        {
            return await _context.Accounts.FindAsync(id);
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            return await _context.Accounts.ToListAsync();
        }

        public async Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
    }
}
