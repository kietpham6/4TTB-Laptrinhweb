using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Thitructuyen.Data;
using Thitructuyen.Helpers;
using Thitructuyen.Models;

namespace Thitructuyen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ApiController(ApplicationDbContext context) => _context = context;

        private int? CurrentUserId()
        {
            var v = User.FindFirst("UserId")?.Value;
            return int.TryParse(v, out var id) ? id : (int?)null;
        }

        // ========== USER MANAGEMENT (Admin) ==========

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUsers(string? role = null, string? status = null, string? search = null)
        {
            var q = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(role)) q = q.Where(u => u.Role == role);
            if (!string.IsNullOrEmpty(status)) q = q.Where(u => u.Status == status);
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                q = q.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s) || u.Username.ToLower().Contains(s));
            }

            var result = q.OrderBy(u => u.Id).Select(u => new
            {
                id = u.Id,
                username = u.Username,
                fullname = u.FullName,
                email = u.Email,
                role = u.Role,
                status = u.Status,
                avatar = string.IsNullOrEmpty(u.AvatarUrl) ? "/Temp/images/avatar/default-avatar.png" : u.AvatarUrl,
                createdAt = u.CreatedAt.ToString("yyyy-MM-dd")
            }).ToList();

            return Ok(result);
        }

        [HttpPost("user")]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser([FromBody] JsonElement data)
        {
            try
            {
                var username = data.GetProperty("username").GetString() ?? "";
                var fullname = data.GetProperty("fullname").GetString() ?? "";
                var email = data.GetProperty("email").GetString() ?? "";
                var role = data.GetProperty("role").GetString() ?? "Student";
                var password = data.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

                if (string.IsNullOrEmpty(password) || password.Length < 6) // R03
                    return Ok(new { success = false, message = "Mật khẩu tối thiểu 6 ký tự!" });
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) // R02
                    return Ok(new { success = false, message = "Email không đúng định dạng!" });
                if (_context.Users.Any(u => u.Username == username)) // R01
                    return Ok(new { success = false, message = "Tên đăng nhập đã tồn tại!" });

                var user = new User
                {
                    Username = username,
                    FullName = fullname,
                    Email = email,
                    Role = role,
                    Password = PasswordHasher.Hash(password),
                    Status = "Active",
                    CreatedAt = DateTime.Now,
                    AvatarUrl = "/Temp/images/avatar/default-avatar.png"
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                var dto = new { id = user.Id, username = user.Username, fullname = user.FullName, email = user.Email, role = user.Role, status = user.Status, avatar = user.AvatarUrl, createdAt = user.CreatedAt.ToString("yyyy-MM-dd") };
                return Ok(new { success = true, message = "Thêm người dùng thành công!", user = dto });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPut("user/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateUser(int id, [FromBody] JsonElement data)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null) return Ok(new { success = false, message = "Không tìm thấy người dùng!" });

                if (data.TryGetProperty("fullname", out var f)) user.FullName = f.GetString() ?? user.FullName;
                if (data.TryGetProperty("email", out var e))
                {
                    var email = e.GetString() ?? "";
                    if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        return Ok(new { success = false, message = "Email không đúng định dạng!" });
                    user.Email = email;
                }
                if (data.TryGetProperty("role", out var r)) user.Role = r.GetString() ?? user.Role;
                if (data.TryGetProperty("status", out var s)) user.Status = s.GetString() ?? user.Status;

                _context.SaveChanges();
                return Ok(new { success = true, message = "Cập nhật người dùng thành công!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpDelete("user/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return Ok(new { success = false, message = "Không tìm thấy người dùng!" });
            if (user.Role == "Admin") return Ok(new { success = false, message = "Không thể xóa tài khoản Admin!" });
            _context.Users.Remove(user);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Xóa người dùng thành công!" });
        }

        [HttpPost("user/{id}/lock")]
        [Authorize(Roles = "Admin")]
        public IActionResult LockUser(int id, [FromBody] JsonElement data)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null) return Ok(new { success = false, message = "Không tìm thấy người dùng!" });

                var status = data.GetProperty("status").GetString() ?? "Active";
                user.Status = status;
                if (status == "Active") { user.LockoutEnd = null; user.FailedLoginAttempts = 0; }
                _context.SaveChanges();

                var message = status == "Locked" ? "Đã khóa người dùng!" : "Đã mở khóa người dùng!";
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }


        [HttpGet("subjects")]
        [Authorize]
        public IActionResult GetSubjects()
        {
            var subjects = _context.Subjects.AsNoTracking().OrderBy(s => s.SubjectName).ToList();
            var result = subjects.Select(s => new
            {
                id = s.Id,
                code = s.SubjectCode,
                name = s.SubjectName,
                description = s.Description,
                credits = s.Credits,
                department = s.Department,
                isActive = s.IsActive,
                status = s.IsActive ? "Hoạt động" : "Tạm dừng",
                questionCount = _context.Questions.Count(q => q.SubjectId == s.Id)
            }).ToList();
            return Ok(result);
        }

        [HttpPost("subject")]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateSubjectApi([FromBody] JsonElement data)
        {
            var code = data.TryGetProperty("code", out var c) ? c.GetString()?.Trim() ?? "" : "";
            var name = data.TryGetProperty("name", out var n) ? n.GetString()?.Trim() ?? "" : "";
            var department = data.TryGetProperty("department", out var d) ? d.GetString() ?? "" : "";
            var description = data.TryGetProperty("description", out var ds) ? ds.GetString() ?? "" : "";
            var credits = data.TryGetProperty("credits", out var cr) && cr.TryGetInt32(out var cv) ? cv : 3;
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) return Ok(new { success = false, message = "Vui lòng nhập mã môn và tên môn học!" });
            if (_context.Subjects.Any(s => s.SubjectCode == code)) return Ok(new { success = false, message = "Mã môn học đã tồn tại!" });
            _context.Subjects.Add(new Subject { SubjectCode = code, SubjectName = name, Department = department, Description = description, Credits = credits, IsActive = true });
            _context.SaveChanges();
            return Ok(new { success = true, message = "Thêm môn học thành công!" });
        }

        [HttpPut("subject/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateSubjectApi(int id, [FromBody] JsonElement data)
        {
            var s = _context.Subjects.Find(id);
            if (s == null) return Ok(new { success = false, message = "Không tìm thấy môn học!" });
            if (data.TryGetProperty("code", out var c)) s.SubjectCode = c.GetString() ?? s.SubjectCode;
            if (data.TryGetProperty("name", out var n)) s.SubjectName = n.GetString() ?? s.SubjectName;
            if (data.TryGetProperty("department", out var d)) s.Department = d.GetString() ?? s.Department;
            if (data.TryGetProperty("description", out var ds)) s.Description = ds.GetString() ?? s.Description;
            if (data.TryGetProperty("credits", out var cr) && cr.TryGetInt32(out var cv)) s.Credits = cv;
            if (data.TryGetProperty("isActive", out var ia)) s.IsActive = ia.ValueKind == JsonValueKind.True || (ia.ValueKind == JsonValueKind.String && ia.GetString() == "Hoạt động");
            _context.SaveChanges();
            return Ok(new { success = true, message = "Cập nhật môn học thành công!" });
        }

        [HttpDelete("subject/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteSubjectApi(int id)
        {
            var s = _context.Subjects.Find(id);
            if (s == null) return Ok(new { success = false, message = "Không tìm thấy môn học!" });
            if (_context.Questions.Any(q => q.SubjectId == id) || _context.Exams.Any(e => e.SubjectId == id))
                return Ok(new { success = false, message = "Không thể xóa môn học đã có câu hỏi hoặc kỳ thi. Hãy tạm dừng môn học thay vì xóa." });
            _context.Subjects.Remove(s);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Xóa môn học thành công!" });
        }

        // ========== QUESTION BANK (R10-R16) ==========

        [HttpGet("questions")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult GetQuestions(string? subject = null, string? chapter = null, string? difficulty = null, string? type = null, string? search = null)
        {
            var q = _context.Questions.Include(x => x.Subject).Include(x => x.Chapter).AsQueryable();
            if (!string.IsNullOrEmpty(difficulty)) q = q.Where(x => x.Difficulty == difficulty);
            if (!string.IsNullOrEmpty(type)) q = q.Where(x => x.QuestionType == type);
            if (!string.IsNullOrEmpty(subject)) q = q.Where(x => x.Subject != null && x.Subject.SubjectName == subject);
            if (!string.IsNullOrEmpty(search)) q = q.Where(x => x.Text.ToLower().Contains(search.ToLower()));

            var result = q.OrderBy(x => x.Id).Select(x => new
            {
                id = x.Id,
                content = x.Text,
                subject = x.Subject != null ? x.Subject.SubjectName : "",
                chapter = x.Chapter != null ? x.Chapter.ChapterName : "",
                difficulty = x.Difficulty,
                type = x.QuestionType,
                points = x.Points,
                optionA = x.OptionA,
                optionB = x.OptionB,
                optionC = x.OptionC,
                optionD = x.OptionD,
                correctAnswer = x.CorrectAnswer
            }).ToList();
            return Ok(result);
        }

        [HttpPost("question")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult CreateQuestion([FromBody] JsonElement data)
        {
            try
            {
                int points = data.TryGetProperty("points", out var pp) ? pp.GetInt32() : 1;
                if (points < 1) points = 1; // R14 (lưu nguyên, doc 0.5-10 với câu hỏi tính điểm phần thi)

                var subjectName = data.TryGetProperty("subject", out var sj) ? sj.GetString() : null;
                var subjectId = string.IsNullOrEmpty(subjectName) ? (int?)null
                    : _context.Subjects.FirstOrDefault(s => s.SubjectName == subjectName)?.Id;

                var question = new Question
                {
                    Text = data.GetProperty("content").GetString() ?? "",
                    SubjectId = subjectId,
                    Difficulty = data.TryGetProperty("difficulty", out var d) ? d.GetString() ?? "Trung bình" : "Trung bình",
                    QuestionType = data.TryGetProperty("type", out var t) ? t.GetString() ?? "Trắc nghiệm" : "Trắc nghiệm",
                    Points = points,
                    OptionA = data.TryGetProperty("optionA", out var a) ? a.GetString() ?? "" : "",
                    OptionB = data.TryGetProperty("optionB", out var b) ? b.GetString() ?? "" : "",
                    OptionC = data.TryGetProperty("optionC", out var c) ? c.GetString() ?? "" : "",
                    OptionD = data.TryGetProperty("optionD", out var e) ? e.GetString() ?? "" : "",
                    CorrectAnswer = data.TryGetProperty("correctAnswer", out var ca) ? ca.GetString() ?? "" : ""
                };
                _context.Questions.Add(question);
                _context.SaveChanges();
                return Ok(new { success = true, message = "Thêm câu hỏi thành công!", id = question.Id });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPut("question/{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult UpdateQuestion(int id, [FromBody] JsonElement data)
        {
            var qn = _context.Questions.Find(id);
            if (qn == null) return Ok(new { success = false, message = "Không tìm thấy câu hỏi!" });
            if (data.TryGetProperty("content", out var c)) qn.Text = c.GetString() ?? qn.Text;
            if (data.TryGetProperty("difficulty", out var d)) qn.Difficulty = d.GetString() ?? qn.Difficulty;
            if (data.TryGetProperty("type", out var t)) qn.QuestionType = t.GetString() ?? qn.QuestionType;
            if (data.TryGetProperty("points", out var p)) qn.Points = p.GetInt32();
            if (data.TryGetProperty("optionA", out var a)) qn.OptionA = a.GetString() ?? qn.OptionA;
            if (data.TryGetProperty("optionB", out var b)) qn.OptionB = b.GetString() ?? qn.OptionB;
            if (data.TryGetProperty("optionC", out var cc)) qn.OptionC = cc.GetString() ?? qn.OptionC;
            if (data.TryGetProperty("optionD", out var dd)) qn.OptionD = dd.GetString() ?? qn.OptionD;
            if (data.TryGetProperty("correctAnswer", out var ca)) qn.CorrectAnswer = ca.GetString() ?? qn.CorrectAnswer;
            _context.SaveChanges();
            return Ok(new { success = true, message = "Cập nhật câu hỏi thành công!" });
        }

        [HttpDelete("question/{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult DeleteQuestion(int id)
        {
            var qn = _context.Questions.Find(id);
            if (qn == null) return Ok(new { success = false, message = "Không tìm thấy câu hỏi!" });
            _context.Questions.Remove(qn);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Xóa câu hỏi thành công!" });
        }

        // ========== EXAM MANAGEMENT ==========

        private static string ExamStatus(Exam e)
        {
            var now = DateTime.Now;
            if (now < e.StartTime) return "Chưa bắt đầu";
            if (now > e.EndTime) return "Đã kết thúc";
            return "Đang diễn ra";
        }

        [HttpGet("exams")]
        [Authorize]
        public IActionResult GetExams()
        {
            var exams = _context.Exams
                .Include(e => e.Subject)
                .Include(e => e.Questions)
                .OrderByDescending(e => e.StartTime)
                .ToList();

            var attempts = _context.ExamAttempts.Where(a => a.Score != null || a.SubmitTime != null).ToList();
            var result = exams.Select(e =>
            {
                var examAttempts = attempts.Where(a => a.ExamId == e.Id).ToList();
                var statusText = ExamStatus(e);
                var statusCode = statusText == "Đang diễn ra" ? "ongoing" : statusText == "Đã kết thúc" ? "ended" : "upcoming";
                return new
                {
                    id = e.Id,
                    title = e.Title,
                    name = e.Title,
                    subject = e.Subject != null ? e.Subject.SubjectName : e.Description,
                    duration = e.Duration,
                    durationText = e.Duration + " phút",
                    totalQuestions = e.Questions != null ? e.Questions.Count : 0,
                    questions = e.Questions != null ? e.Questions.Count : 0,
                    startTime = e.StartTime.ToString("yyyy-MM-ddTHH:mm"),
                    endTime = e.EndTime.ToString("yyyy-MM-ddTHH:mm"),
                    startDate = e.StartTime.ToString("dd/MM/yyyy HH:mm"),
                    endDate = e.EndTime.ToString("dd/MM/yyyy HH:mm"),
                    attempts = examAttempts.Count,
                    avgScore = examAttempts.Any(a => a.Score.HasValue) ? Math.Round(examAttempts.Where(a => a.Score.HasValue).Average(a => a.Score!.Value), 1).ToString("0.0") : "-",
                    status = statusCode,
                    statusText,
                    statusClass = statusCode == "ongoing" ? "bg-success" : statusCode == "ended" ? "bg-secondary" : "bg-warning"
                };
            }).ToList();
            return Ok(result);
        }

        [HttpPost("exam")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult CreateExam([FromBody] JsonElement data)
        {
            try
            {
                var title = data.GetProperty("title").GetString() ?? "";
                var duration = data.TryGetProperty("duration", out var d) ? d.GetInt32() : 60;
                var startStr = data.TryGetProperty("startTime", out var st) ? st.GetString() : null;
                var endStr = data.TryGetProperty("endTime", out var et) ? et.GetString() : null;
                DateTime.TryParse(startStr, out var start);
                DateTime.TryParse(endStr, out var end);

                var subjectName = data.TryGetProperty("subject", out var sj) ? sj.GetString() : null;
                var subjectId = string.IsNullOrEmpty(subjectName) ? (int?)null
                    : _context.Subjects.FirstOrDefault(s => s.SubjectName == subjectName)?.Id;

                // R17: trùng tên trong cùng môn học
                if (_context.Exams.Any(x => x.Title == title && x.SubjectId == subjectId))
                    return Ok(new { success = false, message = "Tên đề thi đã tồn tại trong môn học này!" });
                // R19
                if (start != default && end != default && start >= end)
                    return Ok(new { success = false, message = "Ngày bắt đầu phải trước ngày kết thúc!" });

                var exam = new Exam
                {
                    Title = title,
                    Description = subjectName ?? "",
                    Duration = duration,
                    StartTime = start == default ? DateTime.Now : start,
                    EndTime = end == default ? DateTime.Now.AddDays(7) : end,
                    SubjectId = subjectId
                };
                _context.Exams.Add(exam);
                _context.SaveChanges();
                return Ok(new { success = true, message = "Tạo đề thi thành công!", id = exam.Id });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpDelete("exam/{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult DeleteExam(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null) return Ok(new { success = false, message = "Không tìm thấy đề thi!" });
            // R21: không xóa khi đang có thí sinh làm bài
            if (_context.ExamAttempts.Any(a => a.ExamId == id && a.Status == "InProgress"))
                return Ok(new { success = false, message = "Không thể xóa: đang có thí sinh làm bài!" });

            // Dọn dữ liệu phụ thuộc (FK NoAction nên phải xóa thủ công)
            var attemptIds = _context.ExamAttempts.Where(a => a.ExamId == id).Select(a => a.Id).ToList();
            if (attemptIds.Count > 0)
            {
                var ans = _context.Answers.Where(a => attemptIds.Contains(a.ExamAttemptId));
                _context.Answers.RemoveRange(ans);
                _context.ExamAttempts.RemoveRange(_context.ExamAttempts.Where(a => a.ExamId == id));
            }
            _context.Exams.Remove(exam);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Xóa đề thi thành công!" });
        }

        // ========== STUDENT HISTORY ==========

        [HttpGet("history")]
        [Authorize(Roles = "Student")]
        public IActionResult GetHistory(string? subject = null, string? month = null)
        {
            var uid = CurrentUserId();
            if (uid == null) return Ok(new List<object>());

            var attempts = _context.ExamAttempts
                .Include(a => a.Exam).ThenInclude(e => e!.Subject)
                .Where(a => a.StudentId == uid && a.SubmitTime != null)
                .OrderByDescending(a => a.SubmitTime)
                .ToList();

            var result = new List<object>();
            foreach (var a in attempts)
            {
                var subjName = a.Exam?.Subject?.SubjectName ?? a.Exam?.Description ?? "";
                if (!string.IsNullOrEmpty(subject) && subjName != subject) continue;
                var dateStr = a.SubmitTime!.Value.ToString("yyyy-MM-dd");
                if (!string.IsNullOrEmpty(month) && !dateStr.StartsWith(month)) continue;

                // Xếp hạng trong cùng đề (R37)
                var scores = _context.ExamAttempts.Where(x => x.ExamId == a.ExamId && x.Score != null)
                    .Select(x => x.Score!.Value).OrderByDescending(s => s).ToList();
                int total = scores.Count;
                int rank = a.Score != null ? scores.IndexOf(a.Score.Value) + 1 : total;

                result.Add(new
                {
                    id = a.Id,
                    title = a.Exam?.Title ?? "",
                    subject = subjName,
                    date = dateStr,
                    score = Math.Round(a.Score ?? 0, 1),
                    ranking = $"{rank}/{(total == 0 ? 1 : total)}",
                    status = a.Status == "Graded" ? "Hoàn thành" : "Chờ chấm"
                });
            }
            return Ok(result);
        }

        [HttpGet("history/{id}")]
        [Authorize(Roles = "Student")]
        public IActionResult GetHistoryDetail(int id)
        {
            var uid = CurrentUserId();
            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Answers)!.ThenInclude(an => an.Question)
                .FirstOrDefault(a => a.Id == id && a.StudentId == uid);

            if (attempt == null) return NotFound(new { message = "Không tìm thấy bài thi!" });

            var answers = (attempt.Answers ?? new List<Answer>()).Select(an => new
            {
                question = an.Question?.Text ?? "",
                userAnswer = OptionText(an.Question, an.SelectedAnswer),
                correctAnswer = OptionText(an.Question, an.Question?.CorrectAnswer),
                isCorrect = an.IsCorrect
            }).ToList();

            var scores = _context.ExamAttempts.Where(x => x.ExamId == attempt.ExamId && x.Score != null)
                .Select(x => x.Score!.Value).OrderByDescending(s => s).ToList();
            int total = scores.Count;
            int rank = attempt.Score != null ? scores.IndexOf(attempt.Score.Value) + 1 : total;

            var detail = new
            {
                id = attempt.Id,
                title = attempt.Exam?.Title ?? "",
                score = Math.Round(attempt.Score ?? 0, 1),
                ranking = rank.ToString(),
                totalStudents = total == 0 ? 1 : total,
                teacherComment = attempt.Answers != null && attempt.Answers.Any(x => !string.IsNullOrEmpty(x.TeacherComment))
                    ? string.Join(" ", attempt.Answers.Where(x => !string.IsNullOrEmpty(x.TeacherComment)).Select(x => x.TeacherComment))
                    : (attempt.Status == "Graded" ? "Đã chấm xong." : "Đang chờ giáo viên chấm."),
                answers
            };
            return Ok(detail);
        }

        private static string OptionText(Question? q, string? ans)
        {
            if (q == null || string.IsNullOrEmpty(ans)) return ans ?? "";
            return ans switch
            {
                "A" => q.OptionA,
                "B" => q.OptionB,
                "C" => q.OptionC,
                "D" => q.OptionD,
                _ => ans
            };
        }

        // ========== RANKING (R36-R37) ==========

        [HttpGet("ranking")]
        [Authorize]
        public IActionResult GetRanking(string? subject = null, string? period = null)
        {
            var q = _context.ExamAttempts
                .Include(a => a.Student)
                .Include(a => a.Exam)!
                .ThenInclude(e => e.Subject)
                .Where(a => a.Score != null && a.Student != null && a.Student.Role == "Student");

            if (!string.IsNullOrWhiteSpace(subject) && subject != "all")
                q = q.Where(a => a.Exam != null && ((a.Exam.Subject != null && a.Exam.Subject.SubjectName == subject) || a.Exam.Description == subject));

            var now = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(period) && period != "all")
            {
                DateTime from = period switch
                {
                    "month" => new DateTime(now.Year, now.Month, 1),
                    "quarter" => new DateTime(now.Year, (((now.Month - 1) / 3) * 3) + 1, 1),
                    "year" => new DateTime(now.Year, 1, 1),
                    _ => DateTime.MinValue
                };
                if (from > DateTime.MinValue) q = q.Where(a => (a.SubmitTime ?? a.StartTime) >= from);
            }

            var grouped = q.ToList().GroupBy(a => a.Student!)
                .Select(g => new
                {
                    Student = g.Key,
                    Exams = g.Count(),
                    Avg = g.Average(x => x.Score!.Value),
                    Highest = g.Max(x => x.Score!.Value),
                    Lowest = g.Min(x => x.Score!.Value),
                    Violations = g.Sum(x => x.ViolationCount)
                })
                .OrderByDescending(x => x.Avg)
                .ThenByDescending(x => x.Highest)
                .ThenBy(x => x.Violations)
                .ToList();

            var result = grouped.Select((x, i) => new
            {
                rank = i + 1,
                name = string.IsNullOrWhiteSpace(x.Student.FullName) ? x.Student.Username : x.Student.FullName,
                fullName = string.IsNullOrWhiteSpace(x.Student.FullName) ? x.Student.Username : x.Student.FullName,
                exams = x.Exams,
                attempts = x.Exams,
                avgScore = Math.Round(x.Avg, 1),
                highest = Math.Round(x.Highest, 1),
                bestScore = Math.Round(x.Highest, 1),
                lowest = Math.Round(x.Lowest, 1),
                violations = x.Violations,
                trend = "stable",
                trendValue = "0",
                avatar = string.IsNullOrWhiteSpace(x.Student.AvatarUrl) ? "/Temp/images/avatar/default-avatar.png" : x.Student.AvatarUrl
            }).ToList();
            return Ok(result);
        }

        // ========== STATISTICS ==========

        [HttpGet("statistics/overview")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult GetStatisticsOverview()
        {
            var graded = _context.ExamAttempts.Where(a => a.Score != null).ToList();
            int totalAttempts = graded.Count;
            double avg = totalAttempts > 0 ? graded.Average(a => a.Score!.Value) : 0;
            int pass = graded.Count(a => a.Score >= 5); // R38
            int passRate = totalAttempts > 0 ? (int)Math.Round(pass * 100.0 / totalAttempts) : 0;

            // Phân bố điểm: 0-2, 2-4, 4-6, 6-8, 8-10
            var dist = new int[5];
            foreach (var a in graded)
            {
                var s = a.Score!.Value;
                int idx = s >= 8 ? 4 : s >= 6 ? 3 : s >= 4 ? 2 : s >= 2 ? 1 : 0;
                dist[idx]++;
            }

            return Ok(new
            {
                totalUsers = _context.Users.Count(),
                totalExams = _context.Exams.Count(),
                totalAttempts,
                avgScore = Math.Round(avg, 1),
                passRate,
                failRate = 100 - passRate,
                scoreDistribution = dist
            });
        }

        [HttpGet("statistics/top-students")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult GetTopStudents()
        {
            var data = _context.ExamAttempts
                .Include(a => a.Student)
                .Where(a => a.Score != null && a.Student != null && a.Student.Role == "Student")
                .ToList();

            var result = data.GroupBy(a => a.Student!)
                .Select(g => new { S = g.Key, Exams = g.Count(), Avg = g.Average(x => x.Score!.Value), Hi = g.Max(x => x.Score!.Value), Lo = g.Min(x => x.Score!.Value) })
                .OrderByDescending(x => x.Avg)
                .Take(10)
                .Select((x, i) => new { rank = i + 1, name = x.S.FullName, exams = x.Exams, avgScore = Math.Round(x.Avg, 1), highest = Math.Round(x.Hi, 1), lowest = Math.Round(x.Lo, 1) })
                .ToList();
            return Ok(result);
        }

        [HttpGet("statistics/by-subject")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult GetStatisticsBySubject()
        {
            var subjects = _context.Subjects.ToList();
            var result = new List<object>();
            foreach (var s in subjects)
            {
                var examIds = _context.Exams.Where(e => e.SubjectId == s.Id).Select(e => e.Id).ToList();
                var attempts = _context.ExamAttempts.Where(a => examIds.Contains(a.ExamId) && a.Score != null).ToList();
                int n = attempts.Count;
                double avg = n > 0 ? attempts.Average(a => a.Score!.Value) : 0;
                int pass = attempts.Count(a => a.Score >= 5);
                result.Add(new
                {
                    subject = s.SubjectName,
                    exams = examIds.Count,
                    attempts = n,
                    avgScore = Math.Round(avg, 1),
                    passRate = n > 0 ? (int)Math.Round(pass * 100.0 / n) : 0
                });
            }
            return Ok(result);
        }
    }
}
