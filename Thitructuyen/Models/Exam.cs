using System.ComponentModel.DataAnnotations;

namespace Thitructuyen.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Duration { get; set; } = 60; // R18: 15/30/45/60/90/120

        public DateTime StartTime { get; set; } = DateTime.Now;

        public DateTime EndTime { get; set; } = DateTime.Now.AddDays(7);

        // Liên kết môn học (R09)
        public int? SubjectId { get; set; }

        // R23: cấu hình số câu theo độ khó
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }

        public double PassScore { get; set; } = 5.0; // R38/R39

        // Mật khẩu phòng thi (tùy chọn). Để trống/null nếu đề không yêu cầu mật khẩu.
        [StringLength(100)]
        public string? ExamPassword { get; set; }

        public int? CreatedBy { get; set; } // Id giáo viên tạo đề

        // Câu hỏi gắn trực tiếp với đề (giữ cho TakeExam.cshtml dùng Model.Questions)
        public virtual ICollection<Question>? Questions { get; set; }

        public virtual Subject? Subject { get; set; }
    }
}
