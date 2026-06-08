using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Thitructuyen.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        // ĐÃ XÓA Dashboard - chuyển hướng về trang chủ nếu truy cập
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index", "Home");
        }

        // Lịch sử thi
        public IActionResult History()
        {
            ViewData["Title"] = "Lịch sử thi";
            return View();
        }

        // Xếp hạng
        public IActionResult Ranking()
        {
            ViewData["Title"] = "Xếp hạng";
            return View();
        }

        // Thống kê cá nhân
        public IActionResult Statistics()
        {
            ViewData["Title"] = "Thống kê cá nhân";
            return View();
        }

        // Xem chi tiết kết quả
        public IActionResult ResultDetail(int attemptId)
        {
            ViewData["Title"] = "Chi tiết kết quả";
            return View();
        }
        public IActionResult Settings()
        {
            ViewData["Title"] = "Cấu hình hệ thống";
            return View();
        }
    }
}