using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thitructuyen.Models
{
    public class UserAnswer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int QuestionId { get; set; }

        public string SelectedAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.Now;

        [ForeignKey("QuestionId")]
        public virtual Question? Question { get; set; }
    }
}