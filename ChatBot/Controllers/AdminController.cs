using BusinessObject.Entities;
using ChatBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interfaces;
using System.Threading.Tasks;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Linq;
using System.Text.Json;

namespace ChatBot.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUniversityService _universityService;
        private readonly ISubjectService _subjectService;
        private readonly IAccountRepository _accountRepository;
        private readonly ServiceLayer.Interfaces.IAuthService _authService;

        public AdminController(IUniversityService universityService, ISubjectService subjectService, IAccountRepository accountRepository, ServiceLayer.Interfaces.IAuthService authService)
        {
            _universityService = universityService;
            _subjectService = subjectService;
            _accountRepository = accountRepository;
            _authService = authService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Universities()
        {
            var universities = await _universityService.GetUniversities();
            return View(universities);
        }

        public IActionResult CreateUniversity()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUniversity(University university)
        {
            ModelState.Remove("Subjects");
            if (ModelState.IsValid)
            {
                await _universityService.AddUniversity(university);
                return RedirectToAction(nameof(Universities));
            }
            return View(university);
        }

        public async Task<IActionResult> EditUniversity(int id)
        {
            var university = await _universityService.GetUniversityById(id);
            if (university == null)
            {
                return NotFound();
            }
            return View(university);
        }

        [HttpPost]
        public async Task<IActionResult> EditUniversity(University university)
        {
            ModelState.Remove("Subjects");
            if (ModelState.IsValid)
            {
                await _universityService.UpdateUniversity(university);
                return RedirectToAction(nameof(Universities));
            }
            return View(university);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUniversity(int id)
        {
            await _universityService.DeleteUniversity(id);
            return RedirectToAction(nameof(Universities));
        }

        public async Task<IActionResult> Subjects()
        {
            var subjects = await _subjectService.GetSubjects();
            return View(subjects);
        }

        public async Task<IActionResult> CreateSubject()
        {
            var teachers = (await _accountRepository.GetAllUserInformationsAsync()).Where(u => u.Account.Role == BusinessObject.Enums.RoleEnum.Lecture);
            ViewBag.Teachers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(teachers, "Account_id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(Subject subject)
        {
            ModelState.Remove("University");
            if (ModelState.IsValid)
            {
                await _subjectService.AddSubject(subject);
                return RedirectToAction(nameof(Subjects));
            }
            // If validation failed, re-populate the teachers select list and return view
            var teachers = (await _accountRepository.GetAllUserInformationsAsync()).Where(u => u.Account.Role == BusinessObject.Enums.RoleEnum.Lecture);
            ViewBag.Teachers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(teachers, "Account_id", "Name");
            return View(subject);
        }

        public async Task<IActionResult> EditSubject(string id)
        {
            var subject = await _subjectService.GetSubjectById(id);
            if (subject == null)
            {
                return NotFound();
            }
            var teachers = (await _accountRepository.GetAllUserInformationsAsync()).Where(u => u.Account.Role == BusinessObject.Enums.RoleEnum.Lecture);
            ViewBag.Teachers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(teachers, "Account_id", "Name");
            return View(subject);
        }

        // GET: show form to add a student to a subject by email
        public async Task<IActionResult> AddStudentToSubject(string id)
        {
            var subject = await _subjectService.GetSubjectById(id);
            if (subject == null) return NotFound();
            return View(subject);
        }

        // POST: add student to subject by email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentToSubject(string subjectId, string email)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subjectId))
            {
                TempData["Message"] = "Email or subject missing.";
                return RedirectToAction(nameof(Subjects));
            }

            if (!Guid.TryParse(subjectId, out var subjGuid))
            {
                TempData["Message"] = "Invalid subject id.";
                return RedirectToAction(nameof(Subjects));
            }

            var (success, message) = await _subjectService.AddStudentToSubjectAsync(email.Trim(), subjGuid);
            TempData["Message"] = message;
            return RedirectToAction(nameof(Subjects));
        }

        [HttpPost]
        public async Task<IActionResult> EditSubject(Subject subject)
        {
            ModelState.Remove("University");
            if (ModelState.IsValid)
            {
                await _subjectService.UpdateSubject(subject);
                return RedirectToAction(nameof(Subjects));
            }
            return View(subject);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubject(string id)
        {
            await _subjectService.DeleteSubject(id);
            return RedirectToAction(nameof(Subjects));
        }

        public async Task<IActionResult> Users()
        {
            var users = await _accountRepository.GetAllUserInformationsAsync();
            return View(users);
        }

        public IActionResult CreateTeacher()
        {
            return View(new CreateTeacherViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
        {
            // Send OTP to the teacher email
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Vui lòng kiểm tra lại các trường đã nhập.";
                return View(model);
            }

            // Check if email already exists
            var existingUserInfo = await _accountRepository.GetUserInfoByEmailAsync(model.Email);
            if (existingUserInfo != null)
            {
                ModelState.AddModelError("Email", "Email đã tồn tại trong hệ thống.");
                ViewBag.Error = "Email '" + model.Email + "' đã tồn tại trong hệ thống. Vui lòng sử dụng email khác.";
                return View(model);
            }

            try
            {
                // Send OTP to teacher email
                var otpMessage = await _authService.RequestOtpAsync(model.Email);

                // Store pending info to TempData for verification step
                TempData["AdminPendingUsername"] = model.Username;
                TempData["AdminPendingEmail"] = model.Email;
                TempData["AdminPendingPassword"] = model.Password;
                TempData["OtpSentMessage"] = otpMessage;
                return RedirectToAction("AdminVerifyOtp");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi gửi OTP: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(Guid id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account != null)
            {
                account.IsActive = !account.IsActive;
                await _accountRepository.UpdateAsync(account);
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public IActionResult AdminVerifyOtp()
        {
            var pendingEmail = TempData["AdminPendingEmail"]?.ToString();
            var pendingPassword = TempData["AdminPendingPassword"]?.ToString();
            var pendingUsername = TempData["AdminPendingUsername"]?.ToString();

            if (string.IsNullOrEmpty(pendingEmail))
                return RedirectToAction("Users");

            // Re-store for POST
            TempData["AdminPendingEmail"] = pendingEmail;
            TempData["AdminPendingPassword"] = pendingPassword;
            if (!string.IsNullOrEmpty(pendingUsername)) TempData["AdminPendingUsername"] = pendingUsername;

            ViewBag.PendingEmail = pendingEmail;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminResendOtp([FromBody] dynamic payload)
        {
            try
            {
                var email = (string)payload.email;
                if (string.IsNullOrEmpty(email)) return BadRequest("Email missing");
                await _authService.RequestOtpAsync(email);
                return Ok("OTP đã được gửi lại");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public IActionResult AdminCancelPending()
        {
            TempData.Remove("AdminPendingEmail");
            TempData.Remove("AdminPendingPassword");
            TempData.Remove("AdminPendingUsername");
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminVerifyOtpPost(string email, string otpCode)
        {
            var pendingEmail = TempData["AdminPendingEmail"]?.ToString();
            var pendingPassword = TempData["AdminPendingPassword"]?.ToString();
            var pendingUsername = TempData["AdminPendingUsername"]?.ToString();

            if (string.IsNullOrEmpty(pendingEmail) || !string.Equals(pendingEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Message"] = "Pending email mismatch or expired. Please re-initiate Create Teacher.";
                return RedirectToAction("Users");
            }

            var request = new BusinessObject.Dtos.RequestModel.VerifyOtpRequest
            {
                Email = pendingEmail,
                OtpCode = otpCode,
                Password = pendingPassword,
                Username = pendingUsername
            };

            try
            {
            var createResult = await _authService.VerifyOtpAndCreateAccountAsync(request, "Teacher");
                if (!createResult.Success)
                {
                    TempData["Message"] = createResult.Message;
                    return RedirectToAction("Users");
                }

                TempData["Message"] = createResult.Message;
                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error: " + ex.Message;
                return RedirectToAction("Users");
            }
        }
    }
}
