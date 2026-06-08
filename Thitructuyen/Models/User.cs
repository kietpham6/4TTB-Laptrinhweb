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
        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty; // Admin, Teacher, Student

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Thêm trường AvatarUrl
        public string AvatarUrl { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateTime? Birthday { get; set; }
    }
}