using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thitructuyen.Data;
using Thitructuyen.Models;

namespace Thitructuyen.Controllers
{
    public class ExamController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ExamController(ApplicationDbContext context) => _context = context;

        private int? CurrentUserId()
        {
            var v = User.FindFirst("UserId")?.Value;
            return int.TryParse(v, out var id) ? id : (int?)null;
        }

        // Lấy user hiện tại từ DB (xác thực còn tồn tại). Tránh lỗi FK khi cookie cũ
        // trỏ tới user đã bị xóa do seed lại database.
        private User? ResolveCurrentUser()
        {
            var uid = CurrentUserId();
            User? user = uid != null ? _context.Users.Find(uid.Value) : null;
            if (user == null)
            {
                var uname = User.Identity?.Name;
                if (!string.IsNullOrEmpty(uname))
                    user = _context.Users.FirstOrDefault(u => u.Username == uname);
            }
            return user;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Trả về tất cả đề thi (kèm số câu hỏi) để view hiển thị link ID thật + trạng thái.
            var exams = await _context.Exams
                .Include(e => e.Questions)
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();
            return View(exams);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> TakeExam(int id)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null) return NotFound();

            var now = DateTime.Now;
            // R26 / R27
            if (now < exam.StartTime)
            {
                TempData["ExamError"] = "Kỳ thi chưa đến giờ mở!";
                return RedirectToAction("Index");
            }
            if (now > exam.EndTime)
            {
                TempData["ExamError"] = "Kỳ thi đã kết thúc!";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrWhiteSpace(exam.ExamPassword)
                && HttpContext.Session.GetString($"ExamPasswordVerified_{id}") != "1")
            {
                return View("ExamPassword", exam);
            }

            var student = ResolveCurrentUser();
            if (student == null)
            {
                // Cookie cũ trỏ tới tài khoản không còn tồn tại -> đăng nhập lại
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            var uid = student.Id;

            // R25: mỗi student chỉ thi 1 lần/đề
            var existing = _context.ExamAttempts
                .FirstOrDefault(a => a.ExamId == id && a.StudentId == uid && a.Status != "InProgress");
            if (existing != null)
            {
                TempData["ExamError"] = "Bạn đã hoàn thành bài thi này rồi!";
                return RedirectToAction("History", "Student");
            }

            // Tạo (hoặc lấy lại) lần thi đang diễn ra
            var attempt = _context.ExamAttempts
                .FirstOrDefault(a => a.ExamId == id && a.StudentId == uid && a.Status == "InProgress");
            if (attempt == null)
            {
                attempt = new ExamAttempt
                {
                    ExamId = id,
                    StudentId = uid,
                    StartTime = now,
                    Status = "InProgress",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.ExamAttempts.Add(attempt);
                _context.SaveChanges();
            }

            HttpContext.Session.SetInt32("AttemptId", attempt.Id);
            HttpContext.Session.SetInt32("ExamId", id);

            return View(exam);
        }


        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyExamPassword(int id, string password)
        {
            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == id);
            if (exam == null) return NotFound();

            if (string.IsNullOrWhiteSpace(exam.ExamPassword))
            {
                HttpContext.Session.SetString($"ExamPasswordVerified_{id}", "1");
                return RedirectToAction("TakeExam", new { id });
            }

            if ((password ?? string.Empty).Trim() != exam.ExamPassword.Trim())
            {
                TempData["ExamPasswordError"] = "Mật khẩu đề thi không đúng!";
                return RedirectToAction("TakeExam", new { id });
            }

            HttpContext.Session.SetString($"ExamPasswordVerified_{id}", "1");
            return RedirectToAction("TakeExam", new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitExam()
        {
            // FIX lỗi: FormatException "examId was not in a correct format".
            // Không để ASP.NET Core tự bind int examId nữa, vì khi form/JS gửi sai chuỗi "examId"
            // thì action chưa kịp chạy đã văng lỗi 500. Ta đọc form thủ công và fallback từ Session.
            var form = Request.Form;

            int? examId = null;
            var rawExamId = form["examId"].FirstOrDefault();
            if (int.TryParse(rawExamId, out var parsedExamId))
            {
                examId = parsedExamId;
            }
            else
            {
                examId = HttpContext.Session.GetInt32("ExamId");
            }

            if (examId == null || examId <= 0)
            {
                TempData["ExamError"] = "Không xác định được mã đề thi. Vui lòng vào lại bài thi rồi nộp lại.";
                return RedirectToAction("Index");
            }

            var answers = new Dictionary<int, string>();
            foreach (var key in form.Keys)
            {
                // Radio button trong view có dạng: answers[12] = A
                if (!key.StartsWith("answers[", StringComparison.OrdinalIgnoreCase) || !key.EndsWith("]"))
                    continue;

                var idText = key.Substring("answers[".Length, key.Length - "answers[".Length - 1);
                if (int.TryParse(idText, out var questionId))
                {
                    var value = form[key].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        answers[questionId] = value.Trim();
                    }
                }
            }

            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId.Value);
            if (exam == null)
            {
                TempData["ExamError"] = "Đề thi không tồn tại hoặc đã bị xóa.";
                return RedirectToAction("Index");
            }

            var student = ResolveCurrentUser();
            if (student == null)
            {
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            var uid = student.Id;
            var attemptId = HttpContext.Session.GetInt32("AttemptId");
            ExamAttempt? attempt = null;

            if (attemptId != null)
            {
                attempt = await _context.ExamAttempts
                    .Include(a => a.Answers)
                    .FirstOrDefaultAsync(a => a.Id == attemptId.Value && a.StudentId == uid && a.ExamId == exam.Id);
            }

            attempt ??= await _context.ExamAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.ExamId == exam.Id && a.StudentId == uid && a.Status == "InProgress");

            if (attempt == null)
            {
                attempt = new ExamAttempt
                {
                    ExamId = exam.Id,
                    StudentId = uid,
                    StartTime = DateTime.Now,
                    Status = "InProgress",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.ExamAttempts.Add(attempt);
                await _context.SaveChangesAsync();
            }
            else if (attempt.Status != "InProgress")
            {
                TempData["ExamError"] = "Bài thi này đã được nộp trước đó.";
                return RedirectToAction("History", "Student");
            }

            // Nếu vì lỗi mạng/người dùng bấm nộp lại, xóa câu trả lời tạm cũ để tránh lưu trùng.
            if (attempt.Answers != null && attempt.Answers.Any())
            {
                _context.Answers.RemoveRange(attempt.Answers);
            }

            int rawScore = 0, totalPoints = 0;
            bool hasEssay = false;
            var questions = exam.Questions?.OrderBy(q => q.Id).ToList() ?? new List<Question>();

            foreach (var q in questions)
            {
                var points = q.Points > 0 ? q.Points : 1;
                totalPoints += points;
                answers.TryGetValue(q.Id, out var selected);

                bool isEssay = string.Equals(q.QuestionType, "Tự luận", StringComparison.OrdinalIgnoreCase);
                if (isEssay) hasEssay = true;

                bool isCorrect = !isEssay && !string.IsNullOrWhiteSpace(selected) &&
                                 string.Equals(selected.Trim(), (q.CorrectAnswer ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
                if (isCorrect) rawScore += points;

                _context.Answers.Add(new Answer
                {
                    ExamAttemptId = attempt.Id,
                    QuestionId = q.Id,
                    SelectedAnswer = selected,
                    IsCorrect = isCorrect
                });
            }

            double scaled = totalPoints > 0 ? Math.Round(rawScore * 10.0 / totalPoints, 1) : 0;
            attempt.Score = scaled;
            attempt.SubmitTime = DateTime.Now;
            attempt.Status = hasEssay ? "Submitted" : "Graded";

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = uid,
                Action = "SubmitExam",
                Detail = $"Nộp đề #{exam.Id} - {exam.Title}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("AttemptId");
            HttpContext.Session.Remove("ExamId");

            ViewBag.Score = scaled;
            ViewBag.RawScore = rawScore;
            ViewBag.TotalPoints = totalPoints;
            ViewBag.Percentage = totalPoints > 0 ? Math.Round(rawScore * 100.0 / totalPoints, 1) : 0;
            ViewBag.Passed = scaled >= exam.PassScore;
            ViewBag.HasEssay = hasEssay;
            ViewBag.ExamTitle = exam.Title;

            return View("Result");
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult AutoSave()
        {
            var form = Request.Form;
            var rawExamId = form["examId"].FirstOrDefault();
            int? examId = int.TryParse(rawExamId, out var id) ? id : HttpContext.Session.GetInt32("ExamId");
            if (examId == null || examId <= 0)
                return Ok(new { success = false, message = "Không xác định được mã đề thi." });

            var answers = new Dictionary<int, string>();
            foreach (var key in form.Keys)
            {
                if (!key.StartsWith("answers[", StringComparison.OrdinalIgnoreCase) || !key.EndsWith("]"))
                    continue;

                var idText = key.Substring("answers[".Length, key.Length - "answers[".Length - 1);
                if (int.TryParse(idText, out var questionId))
                {
                    var value = form[key].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(value))
                        answers[questionId] = value.Trim();
                }
            }

            HttpContext.Session.SetString("SavedAnswers_" + examId.Value,
                System.Text.Json.JsonSerializer.Serialize(answers));
            return Ok(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public IActionResult LogViolation()
        {
            var form = Request.Form;
            var rawExamId = form["examId"].FirstOrDefault();
            int? examId = int.TryParse(rawExamId, out var parsedExamId) ? parsedExamId : HttpContext.Session.GetInt32("ExamId");
            if (examId == null || examId <= 0)
                return Ok(new { success = false, violationCount = 0, message = "Không xác định được mã đề thi." });

            var violationType = form["violationType"].FirstOrDefault() ?? "Unknown";
            DateTime timestamp = DateTime.Now;
            var rawTimestamp = form["timestamp"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rawTimestamp) && DateTime.TryParse(rawTimestamp, out var parsedTimestamp))
                timestamp = parsedTimestamp;

            var uid = CurrentUserId();
            var attemptId = HttpContext.Session.GetInt32("AttemptId");
            var attempt = attemptId != null
                ? _context.ExamAttempts.Find(attemptId.Value)
                : _context.ExamAttempts.FirstOrDefault(a => a.ExamId == examId.Value && a.StudentId == uid && a.Status == "InProgress");

            if (attempt != null)
            {
                attempt.ViolationCount++;
                attempt.ViolationLog = (attempt.ViolationLog ?? "") + $"[{timestamp:HH:mm:ss}] {violationType}\n";
                _context.SaveChanges();
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = uid,
                Action = "Violation",
                Detail = $"Đề #{examId.Value} - {violationType}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            return Ok(new { success = true, violationCount = attempt?.ViolationCount ?? 0 });
        }

        public IActionResult Result() => View();
    }
}
