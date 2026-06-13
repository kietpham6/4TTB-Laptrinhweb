using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thitructuyen.Data;
using Thitructuyen.Helpers;
using Thitructuyen.Models;

namespace Thitructuyen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context) => _context = context;

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Bảng điều khiển";
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalExams = _context.Exams.Count();
            ViewBag.TotalAttempts = _context.ExamAttempts.Count();
            ViewBag.Violations = _context.ExamAttempts.Sum(a => (int?)a.ViolationCount) ?? 0;
            return View();
        }
        public IActionResult Users() { ViewData["Title"] = "Quản lý người dùng"; return View(); }

        [HttpPost]
        public IActionResult CreateUser(string username, string password, string fullname, string email, string role)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return Json(new { success = false, message = "Mật khẩu tối thiểu 6 ký tự!" });
            if (_context.Users.Any(u => u.Username == username))
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại!" });

            _context.Users.Add(new User
            {
                Username = username,
                Password = PasswordHasher.Hash(password),
                FullName = fullname ?? "",
                Email = email ?? "",
                Role = role ?? "Student",
                Status = "Active",
                AvatarUrl = "/Temp/images/avatar/default-avatar.png"
            });
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult EditUser(int id, string fullname, string email, string role, string status)
        {
            var u = _context.Users.Find(id);
            if (u == null) return Json(new { success = false, message = "Không tìm thấy!" });
            u.FullName = fullname ?? u.FullName;
            u.Email = email ?? u.Email;
            u.Role = role ?? u.Role;
            u.Status = status ?? u.Status;
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var u = _context.Users.Find(id);
            if (u != null && u.Role != "Admin") { _context.Users.Remove(u); _context.SaveChanges(); }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult LockUser(int id)
        {
            var u = _context.Users.Find(id);
            if (u != null)
            {
                u.Status = u.Status == "Active" ? "Locked" : "Active";
                if (u.Status == "Active") { u.LockoutEnd = null; u.FailedLoginAttempts = 0; }
                _context.SaveChanges();
            }
            return Json(new { success = true });
        }

        // ===== Môn học (R07-R09) =====
        public IActionResult Subjects() { ViewData["Title"] = "Quản lý môn học"; return View(); }

        [HttpPost]
        public IActionResult CreateSubject(string subjectCode, string subjectName, string description, int credits, string department)
        {
            if (_context.Subjects.Any(s => s.SubjectCode == subjectCode)) // R07
                return Json(new { success = false, message = "Mã môn học đã tồn tại!" });

            _context.Subjects.Add(new Subject
            {
                SubjectCode = subjectCode,
                SubjectName = subjectName,
                Description = description ?? "",
                Credits = credits,
                Department = department ?? "",
                IsActive = true
            });
            _context.SaveChanges();
            return Json(new { success = true });
        }

        public IActionResult Chapters(int subjectId) { ViewData["Title"] = "Quản lý chương học"; return View(); }

        [HttpPost]
        public IActionResult CreateChapter(int subjectId, string chapterName, int order)
        {
            _context.Chapters.Add(new Chapter { SubjectId = subjectId, ChapterName = chapterName, Order = order });
            _context.SaveChanges();
            return Json(new { success = true });
        }

        public IActionResult Exams() { ViewData["Title"] = "Quản lý kỳ thi"; return View(); }
        public IActionResult CreateExam() => RedirectToAction("CreateExam", "Teacher");
        public IActionResult QuestionBank() { ViewData["Title"] = "Ngân hàng câu hỏi"; return View(); }

        [HttpPost]
        public IActionResult ImportQuestions(IFormFile file)
        {
            return Json(new { success = false, message = "Hệ thống chỉ hỗ trợ tạo đề từ file Word .docx tại chức năng Tạo kỳ thi mới." });
        }

        public IActionResult Settings() { ViewData["Title"] = "Cấu hình hệ thống"; return View(); }
        public IActionResult Statistics() { ViewData["Title"] = "Thống kê báo cáo"; return View(); }
        public IActionResult Leaderboard() { ViewData["Title"] = "Bảng xếp hạng"; return View(); }
        public IActionResult Proctoring() { ViewData["Title"] = "Giám sát thi"; return View(); }
        public IActionResult ActivityLogs() { ViewData["Title"] = "Nhật ký hoạt động"; return View(); }
    }
}
