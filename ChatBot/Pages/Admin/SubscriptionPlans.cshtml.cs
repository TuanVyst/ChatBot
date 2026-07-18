using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SubscriptionPlansModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionPlansModel(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        public List<SubscriptionPlan> Plans { get; set; } = new();

        [BindProperty]
        public SubscriptionPlan InputPlan { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Plans = await _subscriptionService.GetAllPlansAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu nhập vào không hợp lệ.";
                return RedirectToPage();
            }

            try
            {
                await _subscriptionService.CreatePlanAsync(new SubscriptionPlan
                {
                    Name = InputPlan.Name,
                    Price = InputPlan.Price,
                    DurationDays = InputPlan.DurationDays,
                    DailyTokenLimit = InputPlan.DailyTokenLimit,
                    Description = InputPlan.Description,
                    IsActive = true
                });

                TempData["Message"] = "Tạo gói dịch vụ mới thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int id)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                TempData["Error"] = "Không tìm thấy gói dịch vụ cần chỉnh sửa.";
                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu nhập vào không hợp lệ.";
                return RedirectToPage();
            }

            try
            {
                plan.Name = InputPlan.Name;
                plan.Price = InputPlan.Price;
                plan.DurationDays = InputPlan.DurationDays;
                plan.DailyTokenLimit = InputPlan.DailyTokenLimit;
                plan.Description = InputPlan.Description;
                plan.IsActive = InputPlan.IsActive;

                await _subscriptionService.UpdatePlanAsync(plan);
                TempData["Message"] = "Cập nhật gói dịch vụ thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy gói dịch vụ." });
            }

            try
            {
                plan.IsActive = !plan.IsActive;
                await _subscriptionService.UpdatePlanAsync(plan);
                return new JsonResult(new { success = true, isActive = plan.IsActive });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}
