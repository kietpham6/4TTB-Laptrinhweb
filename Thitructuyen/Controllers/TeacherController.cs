using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Thitructuyen.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        // ĐÃ XÓA Dashboard - chuyển hướng về trang chủ nếu truy cập
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index", "Home");
        }

        // Ngân hàng câu hỏi
        public IActionResult QuestionBank()
        {
            ViewData["Title"] = "Ngân hàng câu hỏi";
            return View();
        }

        // Quản lý đề thi
        public IActionResult Exams()
        {
            ViewData["Title"] = "Quản lý đề thi";
            return View();
        }

        // Tạo đề thi mới
        public IActionResult CreateExam()
        {
            ViewData["Title"] = "Tạo đề thi mới";
            return View();
        }

        [HttpPost]
        public IActionResult CreateExam(string title, string description, int subjectId, int duration,
            DateTime startTime, DateTime endTime, int easyCount, int mediumCount, int hardCount)
        {
            return Json(new { success = true });
        }

        // Chấm bài thi
        public IActionResult Grading()
        {
            ViewData["Title"] = "Chấm bài thi";
            return View();
        }

        // Thống kê kết quả
        public IActionResult Statistics()
        {
            ViewData["Title"] = "Thống kê kết quả";
            return View();
        }
        public IActionResult Settings()
        {
            ViewData["Title"] = "Cấu hình hệ thống";
            return View();
        }
    }

}