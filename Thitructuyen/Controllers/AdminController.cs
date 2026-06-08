using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Thitructuyen.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // Dashboard - Tổng quan hệ thống
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Bảng điều khiển";
            return View();
        }

        // Quản lý người dùng
        public IActionResult Users()
        {
            ViewData["Title"] = "Quản lý người dùng";
            return View();
        }

        [HttpPost]
        public IActionResult CreateUser(string username, string password, string fullname, string email, string role)
        {
            // Logic thêm user
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult EditUser(int id, string fullname, string email, string role, string status)
        {
            // Logic sửa user
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            // Logic xóa user
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult LockUser(int id)
        {
            // Logic khóa user
            return Json(new { success = true });
        }

        // Quản lý môn học
        public IActionResult Subjects()
        {
            ViewData["Title"] = "Quản lý môn học";
            return View();
        }

        [HttpPost]
        public IActionResult CreateSubject(string subjectCode, string subjectName, string description, int credits, string department)
        {
            // Logic thêm môn học
            return Json(new { success = true });
        }

        // Quản lý chương học
        public IActionResult Chapters(int subjectId)
        {
            ViewData["Title"] = "Quản lý chương học";
            return View();
        }

        // Quản lý kỳ thi
        public IActionResult Exams()
        {
            ViewData["Title"] = "Quản lý kỳ thi";
            return View();
        }

        // Ngân hàng câu hỏi
        public IActionResult QuestionBank()
        {
            ViewData["Title"] = "Ngân hàng câu hỏi";
            return View();
        }

        [HttpPost]
        public IActionResult ImportQuestions(IFormFile file)
        {
            // Import Excel
            return Json(new { success = true });
        }

        // Cấu hình hệ thống
        public IActionResult Settings()
        {
            ViewData["Title"] = "Cấu hình hệ thống";
            return View();
        }

        // Thống kê báo cáo
        public IActionResult Statistics()
        {
            ViewData["Title"] = "Thống kê báo cáo";
            return View();
        }

        // Giám sát thi trực tuyến
        public IActionResult Proctoring()
        {
            ViewData["Title"] = "Giám sát thi";
            return View();
        }

        // Nhật ký hoạt động
        public IActionResult ActivityLogs()
        {
            ViewData["Title"] = "Nhật ký hoạt động";
            return View();
        }
    }
}