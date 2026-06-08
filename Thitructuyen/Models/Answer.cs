namespace Thitructuyen.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int ExamAttemptId { get; set; }
        public int QuestionId { get; set; }
        public string? SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int? EssayScore { get; set; } // Điểm tự luận
        public string? TeacherComment { get; set; } // Nhận xét của giáo viên

        public virtual ExamAttempt? ExamAttempt { get; set; }
        public virtual Question? Question { get; set; }
    }
}
