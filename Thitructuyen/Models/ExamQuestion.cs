namespace Thitructuyen.Models
{
    public class ExamQuestion
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int QuestionId { get; set; }
        public int Points { get; set; }
        public int Order { get; set; }

        public virtual Exam? Exam { get; set; }
        public virtual Question? Question { get; set; }
    }
}
