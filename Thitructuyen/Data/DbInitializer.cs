using Thitructuyen.Helpers;
using Thitructuyen.Models;

namespace Thitructuyen.Data
{
    // Tự seed dữ liệu mẫu khi DB trống: 3 tài khoản, môn học, đề thi 1 & 2 (kèm câu hỏi)
    // để các trang TakeExam / Exam Index / History chạy được ngay.
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext db)
        {
            // ----- Tài khoản -----
            if (!db.Users.Any())
            {
                db.Users.AddRange(
                    new User { Username = "admin", Password = PasswordHasher.Hash("admin123"), FullName = "Quản trị viên", Email = "admin@example.com", Role = "Admin", Status = "Active", AvatarUrl = "/Temp/images/avatar/admin.jpg" },
                    new User { Username = "teacher", Password = PasswordHasher.Hash("teacher123"), FullName = "Giảng viên Nguyễn Văn A", Email = "teacher@example.com", Role = "Teacher", Status = "Active", AvatarUrl = "/Temp/images/avatar/teacher.jpg" },
                    new User { Username = "student", Password = PasswordHasher.Hash("student123"), FullName = "Nguyễn Văn B", Email = "student@example.com", Role = "Student", Status = "Active", AvatarUrl = "/Temp/images/avatar/student.jpg" }
                );
                db.SaveChanges();
            }

            // ----- Môn học -----
            if (!db.Subjects.Any())
            {
                db.Subjects.AddRange(
                    new Subject { SubjectCode = "WEB101", SubjectName = "Lập trình Web", Description = "HTML, CSS, JS", Credits = 3, Department = "CNTT" },
                    new Subject { SubjectCode = "DB101", SubjectName = "Cơ sở dữ liệu", Description = "SQL", Credits = 3, Department = "CNTT" },
                    new Subject { SubjectCode = "ENG101", SubjectName = "Tiếng Anh", Description = "Tiếng Anh B1", Credits = 2, Department = "Ngoại ngữ" }
                );
                db.SaveChanges();
            }

            var teacherId = db.Users.FirstOrDefault(u => u.Username == "teacher")?.Id;
            var webId = db.Subjects.FirstOrDefault(s => s.SubjectCode == "WEB101")?.Id;
            var dbId = db.Subjects.FirstOrDefault(s => s.SubjectCode == "DB101")?.Id;

            // ----- Đề thi 1 & 2 (Index.cshtml link tới /Exam/TakeExam/1 và /2) -----
            if (!db.Exams.Any())
            {
                var exam1 = new Exam
                {
                    Title = "Kiểm tra giữa kỳ",
                    Description = "Lập trình Web",
                    Duration = 60,
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(30),
                    SubjectId = webId,
                    CreatedBy = teacherId,
                    Questions = new List<Question>
                    {
                        new Question { Text = "HTML là viết tắt của từ gì?", OptionA = "Hyper Text Markup Language", OptionB = "Hyper Tool Markup Language", OptionC = "Home Text Markup Language", OptionD = "High Text Markup Language", CorrectAnswer = "A", Points = 1, SubjectId = webId, Difficulty = "Dễ", QuestionType = "Trắc nghiệm" },
                        new Question { Text = "CSS dùng để làm gì?", OptionA = "Xử lý dữ liệu", OptionB = "Trang trí giao diện", OptionC = "Kết nối database", OptionD = "Xử lý form", CorrectAnswer = "B", Points = 1, SubjectId = webId, Difficulty = "Dễ", QuestionType = "Trắc nghiệm" },
                        new Question { Text = "Thẻ nào tạo liên kết trong HTML?", OptionA = "<link>", OptionB = "<a>", OptionC = "<href>", OptionD = "<url>", CorrectAnswer = "B", Points = 1, SubjectId = webId, Difficulty = "Trung bình", QuestionType = "Trắc nghiệm" }
                    }
                };
                var exam2 = new Exam
                {
                    Title = "Ôn tập cuối kỳ",
                    Description = "Cơ sở dữ liệu",
                    Duration = 90,
                    StartTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(30),
                    SubjectId = dbId,
                    CreatedBy = teacherId,
                    Questions = new List<Question>
                    {
                        new Question { Text = "Câu lệnh SQL nào dùng để truy vấn dữ liệu?", OptionA = "INSERT", OptionB = "UPDATE", OptionC = "SELECT", OptionD = "DELETE", CorrectAnswer = "C", Points = 1, SubjectId = dbId, Difficulty = "Trung bình", QuestionType = "Trắc nghiệm" },
                        new Question { Text = "Khóa chính (Primary Key) có thể null?", OptionA = "Có", OptionB = "Không", OptionC = "Tùy CSDL", OptionD = "Chỉ với số", CorrectAnswer = "B", Points = 1, SubjectId = dbId, Difficulty = "Dễ", QuestionType = "Trắc nghiệm" }
                    }
                };
                db.Exams.AddRange(exam1, exam2);
                db.SaveChanges();
            }
            // ----- Lượt thi mẫu lưu trong DB thật để Dashboard/Chart/Leaderboard không dùng số cứng -----
            if (!db.ExamAttempts.Any())
            {
                var studentId = db.Users.FirstOrDefault(u => u.Username == "student")?.Id;
                var exams = db.Exams.OrderBy(e => e.Id).Take(3).ToList();
                if (studentId.HasValue && exams.Any())
                {
                    var seedAttempts = new List<ExamAttempt>();
                    var scores = new[] { 10.0, 6.7, 0.2 };
                    for (int i = 0; i < exams.Count; i++)
                    {
                        seedAttempts.Add(new ExamAttempt
                        {
                            ExamId = exams[i].Id,
                            StudentId = studentId.Value,
                            StartTime = DateTime.Now.AddDays(-(i + 1)).AddMinutes(-45),
                            SubmitTime = DateTime.Now.AddDays(-(i + 1)),
                            Score = scores[Math.Min(i, scores.Length - 1)],
                            Status = "Graded",
                            ViolationCount = i == 2 ? 1 : 0,
                            ViolationLog = i == 2 ? "Rời khỏi tab khi làm bài" : null,
                            IpAddress = "127.0.0.1"
                        });
                    }
                    db.ExamAttempts.AddRange(seedAttempts);
                    db.SaveChanges();

                    db.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = studentId.Value,
                        Action = "SeedData",
                        Detail = "Khởi tạo dữ liệu lượt thi mẫu trong database",
                        IpAddress = "127.0.0.1",
                        CreatedAt = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }

        }
    }
}
