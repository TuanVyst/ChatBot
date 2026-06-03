using BusinessObject.Entities;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interfaces;
using System.Threading.Tasks;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Linq;

namespace ChatBot.Controllers
{
    public class AdminController : Controller
    {
        private readonly IUniversityService _universityService;
        private readonly ISubjectService _subjectService;
        private readonly IAccountRepository _accountRepository;

        public AdminController(IUniversityService universityService, ISubjectService subjectService, IAccountRepository accountRepository)
        {
            _universityService = universityService;
            _subjectService = subjectService;
            _accountRepository = accountRepository;
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
            var teachers = (await _accountRepository.GetAllUserInformationsAsync()).Where(u => u.Account.Role == BusinessObject.Enums.RoleEnum.Teacher);
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
            return View(subject);
        }

        public async Task<IActionResult> EditSubject(int id)
        {
            var subject = await _subjectService.GetSubjectById(id);
            if (subject == null)
            {
                return NotFound();
            }
            var teachers = (await _accountRepository.GetAllUserInformationsAsync()).Where(u => u.Account.Role == BusinessObject.Enums.RoleEnum.Teacher);
            ViewBag.Teachers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(teachers, "Account_id", "Name");
            return View(subject);
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
        public async Task<IActionResult> DeleteSubject(int id)
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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeacher(Account account)
        {
            if (ModelState.IsValid)
            {
                account.Role = BusinessObject.Enums.RoleEnum.Teacher;
                var userInfo = new UserInformation
                {
                    User_id = Guid.NewGuid(),
                    Account_id = account.Account_id,
                    Email = account.Username,
                    Name = account.Username.Split('@')[0]
                };
                await _accountRepository.CreateAccountWithUserInfoAsync(account, userInfo);
                return RedirectToAction(nameof(Users));
            }
            return View(account);
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
    }
}
