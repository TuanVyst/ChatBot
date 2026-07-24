using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public int TotalUsers { get; set; }
    public int TotalSubjects { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
    public int TotalChats { get; set; }
    public int TotalTokensThisMonth { get; set; }

    // New financial and subscription stats
    public long TotalRevenueAllTime { get; set; }
    public int TotalRevenueThisMonth { get; set; }
    public int ActiveSubscriptionsCount { get; set; }
    public double PaymentConversionRate { get; set; }

    // Time filter properties
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string RangeType { get; set; } = "this_month";

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? StartDate { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? EndDate { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? SelectedMonth { get; set; }

    public string FilterPeriodLabel { get; set; } = string.Empty;
    public int TotalTokensInPeriod { get; set; }
    public int TotalChatsInPeriod { get; set; }
    public long TotalRevenueInPeriod { get; set; }
    public int TotalNewUsersInPeriod { get; set; }

    // JSON strings for Chart.js
    public string DailyChatsAndTokensJson { get; set; } = "[]";
    public string SubscriptionPlanDistributionJson { get; set; } = "[]";
    public string TopSubjectsJson { get; set; } = "[]";
    public string UserGrowthJson { get; set; } = "[]";

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
        var today = DateTime.UtcNow.Date;

        DateTime fromUtc;
        DateTime toUtc;

        switch (RangeType?.ToLower())
        {
            case "last_month":
                var firstDayLastMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
                var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddTicks(-1);
                fromUtc = firstDayLastMonth;
                toUtc = lastDayLastMonth;
                FilterPeriodLabel = $"Tháng trước ({fromUtc:dd/MM/yyyy} - {toUtc:dd/MM/yyyy})";
                break;

            case "7days":
                fromUtc = today.AddDays(-6);
                toUtc = today.AddDays(1).AddTicks(-1);
                FilterPeriodLabel = $"7 ngày qua ({fromUtc:dd/MM/yyyy} - {today:dd/MM/yyyy})";
                break;

            case "30days":
                fromUtc = today.AddDays(-29);
                toUtc = today.AddDays(1).AddTicks(-1);
                FilterPeriodLabel = $"30 ngày qua ({fromUtc:dd/MM/yyyy} - {today:dd/MM/yyyy})";
                break;

            case "month":
                if (!string.IsNullOrEmpty(SelectedMonth) && DateTime.TryParseExact(SelectedMonth + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedMonth))
                {
                    fromUtc = DateTime.SpecifyKind(parsedMonth, DateTimeKind.Utc);
                    toUtc = fromUtc.AddMonths(1).AddTicks(-1);
                    FilterPeriodLabel = $"Tháng {fromUtc:MM/yyyy} ({fromUtc:dd/MM/yyyy} - {toUtc:dd/MM/yyyy})";
                }
                else
                {
                    RangeType = "this_month";
                    fromUtc = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    toUtc = fromUtc.AddMonths(1).AddTicks(-1);
                    FilterPeriodLabel = $"Tháng này ({fromUtc:dd/MM/yyyy} - {today:dd/MM/yyyy})";
                }
                break;

            case "custom":
                bool hasStart = DateTime.TryParseExact(StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var startParsed);
                bool hasEnd = DateTime.TryParseExact(EndDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var endParsed);

                if (hasStart && hasEnd)
                {
                    fromUtc = DateTime.SpecifyKind(startParsed, DateTimeKind.Utc);
                    toUtc = DateTime.SpecifyKind(endParsed, DateTimeKind.Utc).AddDays(1).AddTicks(-1);
                }
                else if (hasStart)
                {
                    fromUtc = DateTime.SpecifyKind(startParsed, DateTimeKind.Utc);
                    toUtc = today.AddDays(1).AddTicks(-1);
                }
                else if (hasEnd)
                {
                    fromUtc = DateTime.UnixEpoch;
                    toUtc = DateTime.SpecifyKind(endParsed, DateTimeKind.Utc).AddDays(1).AddTicks(-1);
                }
                else
                {
                    RangeType = "this_month";
                    fromUtc = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    toUtc = fromUtc.AddMonths(1).AddTicks(-1);
                }
                FilterPeriodLabel = $"Tùy chỉnh ({fromUtc:dd/MM/yyyy} - {(toUtc > now ? today : toUtc):dd/MM/yyyy})";
                break;

            case "all":
                fromUtc = DateTime.UnixEpoch;
                toUtc = DateTime.UtcNow.AddYears(100);
                FilterPeriodLabel = "Tất cả thời gian";
                break;

            case "this_month":
            default:
                RangeType = "this_month";
                fromUtc = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                toUtc = fromUtc.AddMonths(1).AddTicks(-1);
                FilterPeriodLabel = $"Tháng này ({fromUtc:dd/MM/yyyy} - {today:dd/MM/yyyy})";
                break;
        }

        // Standard overall stats
        TotalUsers = await _context.Accounts.CountAsync();
        TotalSubjects = await _context.Subjects.CountAsync();
        TotalDocuments = await _context.Documents.CountAsync();
        TotalChunks = await _context.DocumentChunks.CountAsync();
        TotalChats = await _context.ChatHistories.CountAsync();

        // Period-filtered stats
        TotalTokensInPeriod = await _context.ChatHistories
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
            .SumAsync(x => (int?)x.TotalTokens) ?? 0;
        TotalTokensThisMonth = TotalTokensInPeriod;

        TotalRevenueAllTime = await _context.PaymentTransactions
            .Where(x => x.Status == BusinessObject.Enums.PaymentStatus.Paid)
            .SumAsync(x => (long?)x.Amount) ?? 0;

        TotalRevenueInPeriod = await _context.PaymentTransactions
            .Where(x => x.Status == BusinessObject.Enums.PaymentStatus.Paid && 
                        x.CreatedAt >= fromUtc && 
                        x.CreatedAt <= toUtc)
            .SumAsync(x => (long?)x.Amount) ?? 0;
        TotalRevenueThisMonth = (int)TotalRevenueInPeriod;

        TotalChatsInPeriod = await _context.ChatHistories
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
            .CountAsync();

        TotalNewUsersInPeriod = await _context.Accounts
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
            .CountAsync();

        ActiveSubscriptionsCount = await _context.Subscriptions
            .Where(x => x.Status == BusinessObject.Enums.SubscriptionStatus.Active && x.EndDate >= now)
            .CountAsync();

        var totalTransactions = await _context.PaymentTransactions.CountAsync();
        var paidTransactions = await _context.PaymentTransactions
            .Where(x => x.Status == BusinessObject.Enums.PaymentStatus.Paid)
            .CountAsync();
        PaymentConversionRate = totalTransactions > 0 
            ? Math.Round((double)paidTransactions / totalTransactions * 100, 2) 
            : 0;

        // In-memory grouping for daily chat & token stats
        var chatData = await _context.ChatHistories
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
            .Select(x => new { x.CreatedAt, x.TotalTokens })
            .ToListAsync();

        var dailyChatsAndTokens = chatData
            .GroupBy(x => x.CreatedAt.ToLocalTime().Date)
            .Select(g => new {
                Date = g.Key.ToString("yyyy-MM-dd"),
                ChatCount = g.Count(),
                TokenCount = g.Sum(x => x.TotalTokens)
            })
            .OrderBy(x => x.Date)
            .ToList();
        DailyChatsAndTokensJson = System.Text.Json.JsonSerializer.Serialize(dailyChatsAndTokens);

        var subPlans = await (from pt in _context.PaymentTransactions
                             join plan in _context.SubscriptionPlans on pt.PlanId equals plan.Id
                             where pt.Status == BusinessObject.Enums.PaymentStatus.Paid &&
                                   pt.CreatedAt >= fromUtc && pt.CreatedAt <= toUtc
                             group pt by plan.Name into g
                             select new {
                                 PlanName = g.Key,
                                 Count = g.Count(),
                                 Revenue = g.Sum(x => x.Amount)
                             })
                             .ToListAsync();
        SubscriptionPlanDistributionJson = System.Text.Json.JsonSerializer.Serialize(subPlans);

        var topSubjects = await (from ch in _context.ChatHistories
                                join sub in _context.Subjects on ch.SubjectId equals sub.Id
                                where ch.CreatedAt >= fromUtc && ch.CreatedAt <= toUtc
                                group ch by new { sub.Code, sub.Name } into g
                                select new {
                                    SubjectCode = g.Key.Code,
                                    SubjectName = g.Key.Name,
                                    ChatCount = g.Count()
                                })
                                .OrderByDescending(x => x.ChatCount)
                                .Take(5)
                                .ToListAsync();
        TopSubjectsJson = System.Text.Json.JsonSerializer.Serialize(topSubjects);

        var userData = await _context.Accounts
            .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt <= toUtc)
            .Select(x => new { x.CreatedAt })
            .ToListAsync();

        var userGrowth = userData
            .GroupBy(x => x.CreatedAt.ToLocalTime().Date)
            .Select(g => new {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();
        UserGrowthJson = System.Text.Json.JsonSerializer.Serialize(userGrowth);

        UserTokenUsage = await (
            from chat in _context.ChatHistories
            join account in _context.Accounts
                on chat.UserId equals account.Account_id.ToString()
            join userInfo in _context.UserInformations
                on account.Account_id equals userInfo.Account_id
            where chat.CreatedAt >= fromUtc && chat.CreatedAt <= toUtc
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
            where chat.CreatedAt >= fromUtc && chat.CreatedAt <= toUtc
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