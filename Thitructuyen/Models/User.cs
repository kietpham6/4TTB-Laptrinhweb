using System.ComponentModel.DataAnnotations;

namespace Thitructuyen.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // Lưu dạng đã băm PBKDF2

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Student"; // Admin, Teacher, Student

        public string Status { get; set; } = "Active"; // Active, Locked

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string AvatarUrl { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateTime? Birthday { get; set; }

        // R06: khóa tạm khi sai mật khẩu 5 lần
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }
    }
}
