using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Thitructuyen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        // ========== USER MANAGEMENT ==========

        [HttpGet("users")]
        public IActionResult GetUsers(string? role = null, string? status = null, string? search = null)
        {
            // Demo data - thực tế lấy từ database
            var users = new List<object>
            {
                new { id = 1, username = "admin", fullname = "Quản trị viên", email = "admin@example.com", role = "Admin", status = "Active", avatar = "/Temp/images/avatar/admin.jpg", createdAt = "2024-01-01" },
                new { id = 2, username = "teacher", fullname = "Giảng viên A", email = "teacher@example.com", role = "Teacher", status = "Active", avatar = "/Temp/images/avatar/teacher.jpg", createdAt = "2024-01-15" },
                new { id = 3, username = "student", fullname = "Nguyễn Văn B", email = "student@example.com", role = "Student", status = "Active", avatar = "/Temp/images/avatar/student.jpg", createdAt = "2024-02-10" }
            };

            var result = new List<object>();
            foreach (var user in users)
            {
                var userRole = user.GetType().GetProperty("role")?.GetValue(user)?.ToString();
                var userStatus = user.GetType().GetProperty("status")?.GetValue(user)?.ToString();
                var userFullname = user.GetType().GetProperty("fullname")?.GetValue(user)?.ToString();
                var userEmail = user.GetType().GetProperty("email")?.GetValue(user)?.ToString();

                bool match = true;
                if (!string.IsNullOrEmpty(role) && userRole != role) match = false;
                if (!string.IsNullOrEmpty(status) && userStatus != status) match = false;
                if (!string.IsNullOrEmpty(search) &&
                    !(userFullname != null && userFullname.ToLower().Contains(search.ToLower())) &&
                    !(userEmail != null && userEmail.ToLower().Contains(search.ToLower()))) match = false;

                if (match) result.Add(user);
            }

            return Ok(result);
        }

        [HttpPost("user")]
        public IActionResult CreateUser([FromBody] JsonElement data)
        {
            try
            {
                var username = data.GetProperty("username").GetString();
                var fullname = data.GetProperty("fullname").GetString();
                var email = data.GetProperty("email").GetString();
                var role = data.GetProperty("role").GetString();

                var newId = new Random().Next(100, 999);
                var newUser = new { id = newId, username = username, fullname = fullname, email = email, role = role, status = "Active", avatar = "/Temp/images/avatar/default-avatar.png", createdAt = DateTime.Now.ToString("yyyy-MM-dd") };

                return Ok(new { success = true, message = "Thêm người dùng thành công!", user = newUser });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPut("user/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] JsonElement data)
        {
            try
            {
                var fullname = data.GetProperty("fullname").GetString();
                var email = data.GetProperty("email").GetString();
                var role = data.GetProperty("role").GetString();
                var status = data.GetProperty("status").GetString();

                return Ok(new { success = true, message = "Cập nhật người dùng thành công!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpDelete("user/{id}")]
        public IActionResult DeleteUser(int id)
        {
            return Ok(new { success = true, message = "Xóa người dùng thành công!" });
        }

        [HttpPost("user/{id}/lock")]
        public IActionResult LockUser(int id, [FromBody] JsonElement data)
        {
            try
            {
                var status = data.GetProperty("status").GetString();
                var message = status == "Locked" ? "Đã khóa người dùng!" : "Đã mở khóa người dùng!";
                return Ok(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ========== QUESTION MANAGEMENT ==========

        private static List<dynamic> questions = new List<dynamic>();

        static ApiController()
        {
            // Khởi tạo dữ liệu mẫu
            questions.Add(new { id = 1, content = "HTML là viết tắt của từ gì?", subject = "Lập trình Web", chapter = "HTML/CSS", difficulty = "Dễ", type = "Trắc nghiệm", points = 1, optionA = "Hyper Text Markup Language", optionB = "Hyper Tool Markup Language", optionC = "Home Text Markup Language", optionD = "None", correctAnswer = "A" });
            questions.Add(new { id = 2, content = "CSS dùng để làm gì?", subject = "Lập trình Web", chapter = "HTML/CSS", difficulty = "Dễ", type = "Trắc nghiệm", points = 1, optionA = "Xử lý dữ liệu", optionB = "Trang trí giao diện", optionC = "Kết nối database", optionD = "Xử lý form", correctAnswer = "B" });
            questions.Add(new { id = 3, content = "Trong SQL, câu lệnh nào dùng để truy vấn dữ liệu?", subject = "Cơ sở dữ liệu", chapter = "SQL cơ bản", difficulty = "TB", type = "Trắc nghiệm", points = 1, optionA = "INSERT", optionB = "UPDATE", optionC = "SELECT", optionD = "DELETE", correctAnswer = "C" });
        }

        [HttpGet("questions")]
        public IActionResult GetQuestions(string? subject = null, string? chapter = null, string? difficulty = null, string? type = null, string? search = null)
        {
            var result = new List<dynamic>();
            foreach (var q in questions)
            {
                bool match = true;
                if (!string.IsNullOrEmpty(subject) && q.subject != subject) match = false;
                if (!string.IsNullOrEmpty(chapter) && q.chapter != chapter) match = false;
                if (!string.IsNullOrEmpty(difficulty) && q.difficulty != difficulty) match = false;
                if (!string.IsNullOrEmpty(type) && q.type != type) match = false;
                if (!string.IsNullOrEmpty(search) && !q.content.ToString().ToLower().Contains(search.ToLower())) match = false;

                if (match) result.Add(q);
            }

            return Ok(result);
        }

        [HttpPost("question")]
        public IActionResult CreateQuestion([FromBody] JsonElement data)
        {
            try
            {
                var newId = questions.Count + 1;
                var newQuestion = new
                {
                    id = newId,
                    content = data.GetProperty("content").GetString(),
                    subject = data.GetProperty("subject").GetString(),
                    chapter = data.GetProperty("chapter").GetString(),
                    difficulty = data.GetProperty("difficulty").GetString(),
                    type = data.GetProperty("type").GetString(),
                    points = data.GetProperty("points").GetInt32(),
                    optionA = data.GetProperty("optionA").GetString(),
                    optionB = data.GetProperty("optionB").GetString(),
                    optionC = data.GetProperty("optionC").GetString(),
                    optionD = data.GetProperty("optionD").GetString(),
                    correctAnswer = data.GetProperty("correctAnswer").GetString()
                };

                questions.Add(newQuestion);
                return Ok(new { success = true, message = "Thêm câu hỏi thành công!", question = newQuestion });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPut("question/{id}")]
        public IActionResult UpdateQuestion(int id, [FromBody] JsonElement data)
        {
            return Ok(new { success = true, message = "Cập nhật câu hỏi thành công!" });
        }

        [HttpDelete("question/{id}")]
        public IActionResult DeleteQuestion(int id)
        {
            for (int i = 0; i < questions.Count; i++)
            {
                if (questions[i].id == id)
                {
                    questions.RemoveAt(i);
                    break;
                }
            }
            return Ok(new { success = true, message = "Xóa câu hỏi thành công!" });
        }

        // ========== EXAM MANAGEMENT ==========

        private static List<dynamic> exams = new List<dynamic>();

        static void InitExams()
        {
            exams.Add(new { id = 1, title = "Kiểm tra giữa kỳ", subject = "Lập trình Web", duration = 60, totalQuestions = 50, startTime = "2026-03-15", endTime = "2026-04-15", status = "Đang diễn ra" });
            exams.Add(new { id = 2, title = "Ôn tập cuối kỳ", subject = "Cơ sở dữ liệu", duration = 90, totalQuestions = 80, startTime = "2026-03-20", endTime = "2026-04-20", status = "Sắp diễn ra" });
        }

        [HttpGet("exams")]
        public IActionResult GetExams()
        {
            InitExams();
            return Ok(exams);
        }

        [HttpPost("exam")]
        public IActionResult CreateExam([FromBody] JsonElement data)
        {
            try
            {
                var newId = exams.Count + 1;
                var newExam = new
                {
                    id = newId,
                    title = data.GetProperty("title").GetString(),
                    subject = data.GetProperty("subject").GetString(),
                    duration = data.GetProperty("duration").GetInt32(),
                    totalQuestions = data.GetProperty("totalQuestions").GetInt32(),
                    startTime = data.GetProperty("startTime").GetString(),
                    endTime = data.GetProperty("endTime").GetString(),
                    status = "Sắp diễn ra"
                };
                exams.Add(newExam);
                return Ok(new { success = true, message = "Tạo đề thi thành công!", exam = newExam });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpDelete("exam/{id}")]
        public IActionResult DeleteExam(int id)
        {
            for (int i = 0; i < exams.Count; i++)
            {
                if (exams[i].id == id)
                {
                    exams.RemoveAt(i);
                    break;
                }
            }
            return Ok(new { success = true, message = "Xóa đề thi thành công!" });
        }

        // ========== STUDENT HISTORY ==========

        [HttpGet("history")]
        public IActionResult GetHistory(string? subject = null, string? month = null)
        {
            var history = new List<dynamic>();
            history.Add(new { id = 1, title = "Kiểm tra giữa kỳ", subject = "Lập trình Web", date = "2026-03-15", score = 9.5, ranking = "2/45", status = "Hoàn thành" });
            history.Add(new { id = 2, title = "Bài tập SQL", subject = "Cơ sở dữ liệu", date = "2026-03-20", score = 7.5, ranking = "15/38", status = "Hoàn thành" });
            history.Add(new { id = 3, title = "Đánh giá năng lực", subject = "Tiếng Anh", date = "2026-03-25", score = 8.0, ranking = "8/52", status = "Hoàn thành" });

            var result = new List<dynamic>();
            foreach (var h in history)
            {
                bool match = true;
                if (!string.IsNullOrEmpty(subject) && h.subject != subject) match = false;
                if (!string.IsNullOrEmpty(month) && !h.date.ToString().StartsWith(month)) match = false;
                if (match) result.Add(h);
            }

            return Ok(result);
        }

        [HttpGet("history/{id}")]
        public IActionResult GetHistoryDetail(int id)
        {
            var answers = new List<dynamic>();
            answers.Add(new { question = "HTML là viết tắt của từ gì?", userAnswer = "Hyper Text Markup Language", correctAnswer = "Hyper Text Markup Language", isCorrect = true });
            answers.Add(new { question = "CSS dùng để làm gì?", userAnswer = "Trang trí giao diện", correctAnswer = "Trang trí giao diện", isCorrect = true });
            answers.Add(new { question = "Trong SQL, câu lệnh nào dùng để truy vấn dữ liệu?", userAnswer = "INSERT", correctAnswer = "SELECT", isCorrect = false });

            var detail = new
            {
                id = id,
                title = "Kiểm tra giữa kỳ",
                score = 9.5,
                ranking = "2/45",
                totalStudents = 45,
                teacherComment = "Bài làm tốt! Cần phát huy thêm phần tự luận.",
                answers = answers
            };

            return Ok(detail);
        }

        // ========== RANKING ==========

        [HttpGet("ranking")]
        public IActionResult GetRanking(string? subject = null, string? period = null)
        {
            var rankings = new List<dynamic>();
            rankings.Add(new { rank = 1, name = "Nguyễn Văn A", exams = 12, avgScore = 9.5, highest = 10, lowest = 8.5, trend = "up", avatar = "/Temp/images/avatar/student1.jpg" });
            rankings.Add(new { rank = 2, name = "Trần Thị B", exams = 10, avgScore = 8.9, highest = 9.5, lowest = 8.0, trend = "up", avatar = "/Temp/images/avatar/student2.jpg" });
            rankings.Add(new { rank = 3, name = "Lê Văn C", exams = 8, avgScore = 8.5, highest = 9.0, lowest = 7.5, trend = "down", avatar = "/Temp/images/avatar/student3.jpg" });
            rankings.Add(new { rank = 4, name = "Phạm Thị D", exams = 15, avgScore = 8.2, highest = 9.0, lowest = 7.0, trend = "up", avatar = "/Temp/images/avatar/default-avatar.png" });
            rankings.Add(new { rank = 5, name = "Hoàng Văn E", exams = 6, avgScore = 7.8, highest = 8.5, lowest = 7.0, trend = "down", avatar = "/Temp/images/avatar/default-avatar.png" });

            return Ok(rankings);
        }

        // ========== STATISTICS ==========

        [HttpGet("statistics/overview")]
        public IActionResult GetStatisticsOverview()
        {
            var scoreDistribution = new List<int> { 15, 45, 120, 89, 234 };

            return Ok(new
            {
                totalUsers = 1234,
                totalExams = 156,
                totalAttempts = 3456,
                avgScore = 7.8,
                passRate = 68,
                failRate = 32,
                scoreDistribution = scoreDistribution
            });
        }

        [HttpGet("statistics/top-students")]
        public IActionResult GetTopStudents()
        {
            var topStudents = new List<dynamic>();
            topStudents.Add(new { rank = 1, name = "Nguyễn Văn A", exams = 12, avgScore = 9.5, highest = 10, lowest = 8.5 });
            topStudents.Add(new { rank = 2, name = "Trần Thị B", exams = 10, avgScore = 8.9, highest = 9.5, lowest = 8.0 });
            topStudents.Add(new { rank = 3, name = "Lê Văn C", exams = 8, avgScore = 8.5, highest = 9.0, lowest = 7.5 });

            return Ok(topStudents);
        }

        [HttpGet("statistics/by-subject")]
        public IActionResult GetStatisticsBySubject()
        {
            var subjects = new List<dynamic>();
            subjects.Add(new { subject = "Lập trình Web", exams = 25, attempts = 1234, avgScore = 8.2, passRate = 78 });
            subjects.Add(new { subject = "Cơ sở dữ liệu", exams = 18, attempts = 987, avgScore = 7.5, passRate = 65 });
            subjects.Add(new { subject = "Tiếng Anh", exams = 12, attempts = 654, avgScore = 7.8, passRate = 70 });

            return Ok(subjects);
        }
    }
}