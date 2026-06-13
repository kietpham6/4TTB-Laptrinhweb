namespace Thitructuyen.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int ExamAttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public double? EssayScore { get; set; }       // R33: điểm tự luận giáo viên chấm
        public string? TeacherComment { get; set; }   // Nhận xét

        public virtual ExamAttempt? ExamAttempt { get; set; }
        public virtual Question? Question { get; set; }
    }
}
