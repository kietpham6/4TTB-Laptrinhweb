using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Thitructuyen.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            // Demo accounts
            if (username == "admin" && password == "admin123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("FullName", "Quản trị viên"),
                    new Claim("Avatar", "/Temp/images/avatar/admin.jpg")
                };
                await SignInAsync(claims);
                return RedirectToLocal(returnUrl);
            }
            else if (username == "teacher" && password == "teacher123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Teacher"),
                    new Claim("FullName", "Giảng viên Nguyễn Văn A"),
                    new Claim("Avatar", "/Temp/images/avatar/teacher.jpg")
                };
                await SignInAsync(claims);
                return RedirectToLocal(returnUrl);
            }
            else if (username == "student" && password == "student123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim("FullName", "Nguyễn Văn B"),
                    new Claim("Avatar", "/Temp/images/avatar/student.jpg"),
                    new Claim("StudentId", "SV001"),
                    new Claim("Class", "12A1")
                };
                await SignInAsync(claims);
                return RedirectToLocal(returnUrl);
            }

            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        private async Task SignInAsync(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true });
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string fullname, string email, string username, string password, string confirmPassword, string role)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp";
                return View();
            }
            // Logic đăng ký
            ViewBag.Success = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập email!";
                return View();
            }

            // Logic gửi email reset mật khẩu
            ViewBag.Success = "Link đặt lại mật khẩu đã được gửi đến email của bạn!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Profile(string fullname, string email, string phone, string address)
        {
            ViewBag.Success = "Cập nhật thông tin thành công!";
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới xác nhận không khớp";
                return View();
            }
            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}