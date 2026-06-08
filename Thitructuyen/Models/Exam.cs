using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thitructuyen.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Duration { get; set; } = 60;

        public DateTime StartTime { get; set; } = DateTime.Now;

        public DateTime EndTime { get; set; } = DateTime.Now.AddDays(7);

        // Thêm property này - quan trọng!
        public virtual ICollection<Question>? Questions { get; set; }
    }
}