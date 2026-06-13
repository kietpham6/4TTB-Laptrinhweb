using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thitructuyen.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        // Cho phép null: câu hỏi nằm trong "ngân hàng" chưa gán vào đề thi nào.
        public int? ExamId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty; // A/B/C/D hoặc nội dung Đúng/Sai

        public int Points { get; set; } = 1;

        // Thuộc tính ngân hàng câu hỏi (R10-R16)
        public int? SubjectId { get; set; }
        public int? ChapterId { get; set; }
        public string Difficulty { get; set; } = "Trung bình";  // Dễ / Trung bình / Khó
        public string QuestionType { get; set; } = "Trắc nghiệm"; // Trắc nghiệm / Đúng/Sai / Tự luận

        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }

        public virtual Subject? Subject { get; set; }
        public virtual Chapter? Chapter { get; set; }
    }
}
