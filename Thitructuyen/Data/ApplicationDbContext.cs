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

        // THÊM DÒNG NÀY - DbSet cho User
        public DbSet<User> Users { get; set; }

        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UserAnswer> UserAnswers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình mối quan hệ nếu cần
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        }
    }
}