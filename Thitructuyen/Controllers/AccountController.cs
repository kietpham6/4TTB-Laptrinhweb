using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Thitructuyen.Data;
using Thitructuyen.Helpers;
using Thitructuyen.Models;

namespace Thitructuyen.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu";
                return View();
            }

            // R05: tài khoản bị khóa không thể đăng nhập
            if (user.Status == "Locked")
            {
                ViewBag.Error = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.";
                return View();
            }

            // R06: đang trong thời gian khóa tạm 15 phút
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                var minutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
                ViewBag.Error = $"Bạn đã nhập sai quá nhiều lần. Thử lại sau {minutes} phút.";
                return View();
            }

            // Kiểm tra mật khẩu
            if (!PasswordHasher.Verify(password, user.Password))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5) // R06
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                    ViewBag.Error = "Sai mật khẩu 5 lần. Tài khoản bị khóa tạm 15 phút.";
                }
                else
                {
                    ViewBag.Error = $"Sai tên đăng nhập hoặc mật khẩu (còn {5 - user.FailedLoginAttempts} lần thử).";
                }
                _context.SaveChanges();
                return View();
            }

            // Đăng nhập thành công -> reset bộ đếm
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            _context.SaveChanges();

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = user.Id,
                Action = "Login",
                Detail = $"{user.Username} đăng nhập",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            _context.SaveChanges();

            await SignInAsync(user);
            return RedirectToLocal(returnUrl, user.Role);
        }

        private async Task SignInAsync(User user)
        {
            var avatar = string.IsNullOrEmpty(user.AvatarUrl) ? "/Temp/images/avatar/default-avatar.png" : user.AvatarUrl;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName),
                new Claim("Avatar", avatar),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Email", user.Email ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true });
        }

        private IActionResult RedirectToLocal(string? returnUrl, string? role)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Teacher" => RedirectToAction("Exams", "Teacher"),
                "Student" => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string fullname, string email, string username, string password, string confirmPassword, string role)
        {
            // R03
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu tối thiểu 6 ký tự";
                return View();
            }
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp";
                return View();
            }
            // R02
            if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ViewBag.Error = "Email không đúng định dạng";
                return View();
            }
            // R01
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại";
                return View();
            }
            // R04: không cho tự đăng ký Admin
            if (role != "Teacher" && role != "Student")
                role = "Student";

            var user = new User
            {
                Username = username,
                Password = PasswordHasher.Hash(password),
                FullName = fullname ?? string.Empty,
                Email = email,
                Role = role,
                Status = "Active",
                CreatedAt = DateTime.Now,
                AvatarUrl = "/Temp/images/avatar/default-avatar.png"
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ForgotPassword(string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập email!";
                return View();
            }
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6) // R03
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                // Không tiết lộ email tồn tại hay không (bảo mật)
                ViewBag.Error = "Không tìm thấy tài khoản với email này!";
                return View();
            }

            user.Password = PasswordHasher.Hash(newPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            _context.SaveChanges();

            ViewBag.Success = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile() => View();

        [HttpPost]
        public IActionResult Profile(string fullname, string email, string phone, string address, DateTime? birthday)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy người dùng!";
                return View();
            }

            user.FullName = fullname ?? user.FullName;
            if (!string.IsNullOrEmpty(email)) user.Email = email;
            user.Phone = phone ?? string.Empty;
            user.Address = address ?? string.Empty;
            if (birthday.HasValue) user.Birthday = birthday;
            _context.SaveChanges();

            ViewBag.Success = "Cập nhật thông tin thành công!";
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy người dùng!";
                return View();
            }
            if (!PasswordHasher.Verify(currentPassword, user.Password))
            {
                ViewBag.Error = "Mật khẩu hiện tại không đúng";
                return View();
            }
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6) // R03
            {
                ViewBag.Error = "Mật khẩu mới tối thiểu 6 ký tự";
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu mới xác nhận không khớp";
                return View();
            }
            user.Password = PasswordHasher.Hash(newPassword);
            _context.SaveChanges();
            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }

        public IActionResult AccessDenied() => View();
    }
}
