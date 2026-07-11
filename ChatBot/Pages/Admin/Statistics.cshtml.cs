using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class StatisticsModel : PageModel
{
    private readonly AppDbContext _context;

    public StatisticsModel(AppDbContext context)
    {
        _context = context;
    }

    public int TotalUsers { get; set; }
    public int TotalSubjects { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
    public int TotalChats { get; set; }
    public int TotalTokensThisMonth { get; set; }

    public List<UserTokenItem> UserTokenUsage { get; set; } = new();
    public List<SubjectTokenItem> SubjectTokenUsage { get; set; } = new();

    public class UserTokenItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalChats { get; set; }
        public int TotalTokens { get; set; }
    }

    public class SubjectTokenItem
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int TotalChats { get; set; }
        public int TotalTokens { get; set; }
    }

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;

        var startOfMonth = new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        var startOfNextMonth = startOfMonth.AddMonths(1);

        TotalUsers = await _context.Accounts.CountAsync();
        TotalSubjects = await _context.Subjects.CountAsync();
        TotalDocuments = await _context.Documents.CountAsync();
        TotalChunks = await _context.DocumentChunks.CountAsync();
        TotalChats = await _context.ChatHistories.CountAsync();

        TotalTokensThisMonth = await _context.ChatHistories
            .Where(x =>
                x.CreatedAt >= startOfMonth &&
                x.CreatedAt < startOfNextMonth)
            .SumAsync(x => (int?)x.TotalTokens) ?? 0;

        UserTokenUsage = await (
            from chat in _context.ChatHistories
            join account in _context.Accounts
                on chat.UserId equals account.Account_id.ToString()
            join userInfo in _context.UserInformations
                on account.Account_id equals userInfo.Account_id
            where chat.CreatedAt >= startOfMonth &&
                  chat.CreatedAt < startOfNextMonth
            group chat by new
            {
                userInfo.Name,
                userInfo.Email
            }
            into grouped
            orderby grouped.Sum(x => x.TotalTokens) descending
            select new UserTokenItem
            {
                UserName = grouped.Key.Name,
                Email = grouped.Key.Email,
                TotalChats = grouped.Count(),
                TotalTokens = grouped.Sum(x => x.TotalTokens)
            })
            .ToListAsync();

        SubjectTokenUsage = await (
            from chat in _context.ChatHistories
            join subject in _context.Subjects
                on chat.SubjectId equals subject.Id
            where chat.CreatedAt >= startOfMonth &&
                  chat.CreatedAt < startOfNextMonth
            group chat by new
            {
                subject.Code,
                subject.Name
            }
            into grouped
            orderby grouped.Sum(x => x.TotalTokens) descending
            select new SubjectTokenItem
            {
                SubjectCode = grouped.Key.Code,
                SubjectName = grouped.Key.Name,
                TotalChats = grouped.Count(),
                TotalTokens = grouped.Sum(x => x.TotalTokens)
            })
            .ToListAsync();
    }
}