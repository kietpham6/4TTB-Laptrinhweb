namespace Thitructuyen.Models
{
    public class ExamAttempt
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public int? Score { get; set; }
        public string? Status { get; set; } // InProgress, Submitted, Graded
        public string? ViolationLog { get; set; } // Log gian lận
        public string? IpAddress { get; set; }

        public virtual Exam? Exam { get; set; }
        public virtual User? Student { get; set; }
    }
}
