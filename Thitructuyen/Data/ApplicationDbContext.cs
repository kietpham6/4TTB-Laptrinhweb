using Microsoft.EntityFrameworkCore;
using Thitructuyen.Models;

namespace Thitructuyen.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Subject>().HasIndex(s => s.SubjectCode).IsUnique();

            // Default DB để script SQL seed (bỏ trống các cột này) chạy không lỗi NULL
            modelBuilder.Entity<User>(b =>
            {
                b.Property(u => u.AvatarUrl).HasDefaultValue("");
                b.Property(u => u.Phone).HasDefaultValue("");
                b.Property(u => u.Address).HasDefaultValue("");
                b.Property(u => u.FullName).HasDefaultValue("");
                b.Property(u => u.Email).HasDefaultValue("");
                b.Property(u => u.Role).HasDefaultValue("Student");
                b.Property(u => u.Status).HasDefaultValue("Active");
                b.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);
                b.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Exam>(b =>
            {
                b.Property(e => e.Description).HasDefaultValue("");
                b.Property(e => e.Duration).HasDefaultValue(60);
                b.Property(e => e.EasyCount).HasDefaultValue(0);
                b.Property(e => e.MediumCount).HasDefaultValue(0);
                b.Property(e => e.HardCount).HasDefaultValue(0);
                b.Property(e => e.PassScore).HasDefaultValue(5.0);
            });

            modelBuilder.Entity<Question>(b =>
            {
                b.Property(q => q.OptionA).HasDefaultValue("");
                b.Property(q => q.OptionB).HasDefaultValue("");
                b.Property(q => q.OptionC).HasDefaultValue("");
                b.Property(q => q.OptionD).HasDefaultValue("");
                b.Property(q => q.Points).HasDefaultValue(1);
                b.Property(q => q.Difficulty).HasDefaultValue("Trung bình");
                b.Property(q => q.QuestionType).HasDefaultValue("Trắc nghiệm");
            });

            modelBuilder.Entity<Subject>(b =>
            {
                b.Property(s => s.Description).HasDefaultValue("");
                b.Property(s => s.Department).HasDefaultValue("");
                b.Property(s => s.IsActive).HasDefaultValue(true);
            });

            // Câu hỏi - Đề thi: tùy chọn, xóa đề thì set null (không xóa câu hỏi trong ngân hàng)
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Subject)
                .WithMany()
                .HasForeignKey(q => q.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Chapter)
                .WithMany()
                .HasForeignKey(q => q.ChapterId)
                .OnDelete(DeleteBehavior.NoAction);

            // Lần thi - Câu trả lời
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.ExamAttempt)
                .WithMany(at => at.Answers)
                .HasForeignKey(a => a.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(a => a.Exam)
                .WithMany()
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
