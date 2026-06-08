using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thitructuyen.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;  // ĐỔI từ Content thành Text

        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;

        public int Points { get; set; } = 1;

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
    }
}