namespace Thitructuyen.Models
{
    public class ExamAttempt
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public double? Score { get; set; }          // Điểm quy đổi thang 10
        public string? Status { get; set; }          // InProgress, Submitted, Graded
        public string? ViolationLog { get; set; }    // Nhật ký gian lận (text)
        public int ViolationCount { get; set; } = 0; // R29/R30
        public string? IpAddress { get; set; }

        public virtual Exam? Exam { get; set; }
        public virtual User? Student { get; set; }
        public virtual ICollection<Answer>? Answers { get; set; }
    }
}
