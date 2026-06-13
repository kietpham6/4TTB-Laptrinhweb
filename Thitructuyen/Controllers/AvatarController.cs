using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;
using Thitructuyen.Data;
using Thitructuyen.Models;

namespace Thitructuyen.Controllers
{
    [Authorize]
    public class AvatarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AvatarController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file ảnh!" });
            }

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif, bmp)!" });
            }

            // Kiểm tra kích thước (tối đa 2MB)
            if (file.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 2MB!" });
            }

            try
            {
                // Lấy username từ session
                var username = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Username == username);

                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng!" });
                }

                // Tạo tên file duy nhất
                var fileName = $"{username}_{DateTime.Now.Ticks}{fileExtension}";
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Lưu file mới
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn avatar trong database
                var avatarPath = $"/uploads/avatars/{fileName}";
                user.AvatarUrl = avatarPath;
                await _context.SaveChangesAsync();

                // Cấp lại cookie đăng nhập với claim Avatar mới (để hiển thị sau khi reload)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", string.IsNullOrEmpty(user.FullName) ? user.Username : user.FullName),
                    new Claim("Avatar", avatarPath),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("Email", user.Email ?? string.Empty)
                };
                var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return Json(new { success = true, message = "Cập nhật ảnh đại diện thành công!", avatarUrl = avatarPath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAvatar(string username)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || string.IsNullOrEmpty(user.AvatarUrl))
            {
                return Json(new { avatarUrl = "" });
            }
            return Json(new { avatarUrl = user.AvatarUrl });
        }
    }
}