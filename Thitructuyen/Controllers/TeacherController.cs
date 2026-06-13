using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Thitructuyen.Data;
using Thitructuyen.Models;

namespace Thitructuyen.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TeacherController(ApplicationDbContext context) => _context = context;

        private int? CurrentUserId()
        {
            var v = User.FindFirst("UserId")?.Value;
            return int.TryParse(v, out var id) ? id : (int?)null;
        }

        public IActionResult Dashboard() => RedirectToAction("Index", "Home");

        public IActionResult QuestionBank() { ViewData["Title"] = "Ngân hàng câu hỏi"; return View(); }
        public IActionResult Exams() { ViewData["Title"] = "Quản lý đề thi"; return View(); }

        [HttpGet]
        public IActionResult CreateExam()
        {
            ViewData["Title"] = "Tạo đề thi mới";
            ViewBag.Subjects = _context.Subjects.Where(s => s.IsActive).OrderBy(s => s.SubjectName).ToList();
            return View();
        }

        // Luồng 4.1: tạo đề + tự động lấy câu hỏi từ ngân hàng theo độ khó (R23)
        [HttpPost]
        public IActionResult CreateExam(string title, string description, int subjectId, int duration,
            string? examPassword, DateTime startTime, DateTime endTime, int easyCount, int mediumCount, int hardCount)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Vui lòng nhập tên đề thi!" });
            if (!_context.Subjects.Any(s => s.Id == subjectId))
                return Json(new { success = false, message = "Vui lòng chọn môn học hợp lệ!" });
            if (duration < 5)
                return Json(new { success = false, message = "Thời gian làm bài không hợp lệ!" });
            // R17
            if (_context.Exams.Any(e => e.Title == title && e.SubjectId == subjectId))
                return Json(new { success = false, message = "Tên đề thi đã tồn tại trong môn học này!" });
            // R19
            if (startTime >= endTime)
                return Json(new { success = false, message = "Ngày bắt đầu phải trước ngày kết thúc!" });

            EnsureExamPasswordColumn();

            var exam = new Exam
            {
                Title = title.Trim(),
                Description = description ?? "",
                Duration = duration,
                ExamPassword = string.IsNullOrWhiteSpace(examPassword) ? null : examPassword.Trim(),
                StartTime = startTime,
                EndTime = endTime,
                SubjectId = subjectId,
                EasyCount = easyCount,
                MediumCount = mediumCount,
                HardCount = hardCount,
                CreatedBy = CurrentUserId()
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            // Rút câu hỏi từ ngân hàng theo độ khó rồi gán vào đề (clone để giữ ngân hàng nguyên vẹn)
            PullQuestions(exam, subjectId, "Dễ", easyCount);
            PullQuestions(exam, subjectId, "Trung bình", mediumCount);
            PullQuestions(exam, subjectId, "Khó", hardCount);
            _context.SaveChanges();

            return Json(new { success = true, message = "Tạo đề thi thành công!", id = exam.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PreviewWordExam(IFormFile wordFile)
        {
            if (wordFile == null || wordFile.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file Word .docx!" });

            var ext = Path.GetExtension(wordFile.FileName).ToLowerInvariant();
            if (ext != ".docx")
                return Json(new { success = false, message = "Hệ thống chỉ hỗ trợ file Word định dạng .docx." });

            if (wordFile.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "File Word không được vượt quá 10MB." });

            try
            {
                using var stream = wordFile.OpenReadStream();
                var questions = ParseQuestionsFromDocx(stream);

                if (questions.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Chưa đọc được câu hỏi nào. Vui lòng kiểm tra đúng mẫu: Câu 1, A/B/C/D, Đáp án."
                    });
                }

                var invalid = questions.Count(q => !q.IsValid);
                return Json(new
                {
                    success = true,
                    message = invalid == 0
                        ? $"Đã đọc được {questions.Count} câu hỏi. Mày kiểm tra lại rồi bấm tạo đề."
                        : $"Đã đọc được {questions.Count} câu hỏi, có {invalid} câu cần kiểm tra trước khi tạo đề.",
                    questions
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể đọc file Word: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateExamFromWord(string title, string description, int subjectId, int duration,
            string? examPassword, DateTime startTime, DateTime endTime, string questionsJson)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Vui lòng nhập tên đề thi!" });
            if (!_context.Subjects.Any(s => s.Id == subjectId))
                return Json(new { success = false, message = "Vui lòng chọn môn học hợp lệ!" });
            if (duration < 5)
                return Json(new { success = false, message = "Thời gian làm bài không hợp lệ!" });
            if (startTime >= endTime)
                return Json(new { success = false, message = "Ngày bắt đầu phải trước ngày kết thúc!" });
            if (_context.Exams.Any(e => e.Title == title.Trim() && e.SubjectId == subjectId))
                return Json(new { success = false, message = "Tên đề thi đã tồn tại trong môn học này!" });
            if (string.IsNullOrWhiteSpace(questionsJson))
                return Json(new { success = false, message = "Chưa có dữ liệu câu hỏi từ file Word!" });

            List<WordQuestionDto>? questions;
            try
            {
                questions = JsonSerializer.Deserialize<List<WordQuestionDto>>(questionsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return Json(new { success = false, message = "Dữ liệu câu hỏi không hợp lệ. Vui lòng tải lại file Word." });
            }

            questions ??= new List<WordQuestionDto>();
            questions = questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).ToList();
            if (questions.Count == 0)
                return Json(new { success = false, message = "Danh sách câu hỏi đang trống!" });

            var invalid = questions.Where(q => !ValidateWordQuestion(q).IsValid).ToList();
            if (invalid.Any())
                return Json(new { success = false, message = $"Còn {invalid.Count} câu hỏi chưa hợp lệ. Vui lòng kiểm tra lại phần preview." });

            EnsureExamPasswordColumn();

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var easy = questions.Count(q => NormalizeDifficulty(q.Difficulty) == "Dễ");
                var medium = questions.Count(q => NormalizeDifficulty(q.Difficulty) == "Trung bình");
                var hard = questions.Count(q => NormalizeDifficulty(q.Difficulty) == "Khó");

                var exam = new Exam
                {
                    Title = title.Trim(),
                    Description = description ?? "",
                    Duration = duration,
                    ExamPassword = string.IsNullOrWhiteSpace(examPassword) ? null : examPassword.Trim(),
                    StartTime = startTime,
                    EndTime = endTime,
                    SubjectId = subjectId,
                    EasyCount = easy,
                    MediumCount = medium,
                    HardCount = hard,
                    CreatedBy = CurrentUserId()
                };

                _context.Exams.Add(exam);
                _context.SaveChanges();

                foreach (var item in questions)
                {
                    _context.Questions.Add(new Question
                    {
                        ExamId = exam.Id,
                        SubjectId = subjectId,
                        Text = item.Text.Trim(),
                        OptionA = item.OptionA?.Trim() ?? "",
                        OptionB = item.OptionB?.Trim() ?? "",
                        OptionC = item.OptionC?.Trim() ?? "",
                        OptionD = item.OptionD?.Trim() ?? "",
                        CorrectAnswer = NormalizeCorrectAnswer(item.CorrectAnswer),
                        Difficulty = NormalizeDifficulty(item.Difficulty),
                        QuestionType = NormalizeQuestionType(item.QuestionType),
                        Points = Math.Clamp(item.Points <= 0 ? 1 : item.Points, 1, 10)
                    });
                }

                _context.SaveChanges();
                transaction.Commit();

                return Json(new
                {
                    success = true,
                    message = $"Tạo đề thi từ file Word thành công! Đã lưu {questions.Count} câu hỏi.",
                    id = exam.Id
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Json(new { success = false, message = "Lỗi khi tạo đề: " + GetFullErrorMessage(ex) });
            }
        }

        private void EnsureExamPasswordColumn()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[Exams]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.Exams', N'ExamPassword') IS NULL
BEGIN
    ALTER TABLE [dbo].[Exams] ADD [ExamPassword] NVARCHAR(100) NULL;
END
");
            }
            catch
            {
                // Không chặn tạo đề. Nếu database chưa cập nhật, catch bên ngoài sẽ trả lỗi chi tiết.
            }
        }

        private static string GetFullErrorMessage(Exception ex)
        {
            var messages = new List<string>();
            var current = ex;
            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                    messages.Add(current.Message);
                current = current.InnerException;
            }

            return string.Join(" | ", messages.Distinct());
        }

        private void PullQuestions(Exam exam, int subjectId, string difficulty, int count)
        {
            if (count <= 0) return;
            var bank = _context.Questions
                .Where(q => q.SubjectId == subjectId && q.Difficulty == difficulty && q.ExamId == null)
                .OrderBy(q => Guid.NewGuid())
                .Take(count)
                .ToList();

            foreach (var src in bank)
            {
                _context.Questions.Add(new Question
                {
                    ExamId = exam.Id,
                    Text = src.Text,
                    OptionA = src.OptionA, OptionB = src.OptionB, OptionC = src.OptionC, OptionD = src.OptionD,
                    CorrectAnswer = src.CorrectAnswer,
                    Points = src.Points,
                    SubjectId = src.SubjectId,
                    ChapterId = src.ChapterId,
                    Difficulty = src.Difficulty,
                    QuestionType = src.QuestionType
                });
            }
        }

        private static List<WordQuestionDto> ParseQuestionsFromDocx(Stream stream)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var document = archive.GetEntry("word/document.xml")
                ?? throw new InvalidOperationException("File .docx không hợp lệ hoặc thiếu nội dung document.xml.");

            using var reader = new StreamReader(document.Open(), Encoding.UTF8);
            var xml = XDocument.Parse(reader.ReadToEnd());
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var lines = xml.Descendants(w + "p")
                .Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(CleanLine)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var result = new List<WordQuestionDto>();
            WordQuestionDto? current = null;

            foreach (var line in lines)
            {
                var questionMatch = Regex.Match(line, @"^(?:Câu|Cau|Question)\s*\d*\s*[:.)-]?\s*(.+)$", RegexOptions.IgnoreCase);
                var numberQuestionMatch = Regex.Match(line, @"^\d+\s*[.)-]\s+(.+)$");

                if (questionMatch.Success || numberQuestionMatch.Success)
                {
                    if (current != null) AddValidated(result, current);
                    current = new WordQuestionDto
                    {
                        Text = (questionMatch.Success ? questionMatch.Groups[1].Value : numberQuestionMatch.Groups[1].Value).Trim(),
                        QuestionType = "Trắc nghiệm",
                        Difficulty = "Trung bình",
                        Points = 1
                    };
                    continue;
                }

                if (current == null) continue;

                var optionMatch = Regex.Match(line, @"^([A-Da-d])\s*[.)-]\s*(.+)$");
                if (optionMatch.Success)
                {
                    var key = optionMatch.Groups[1].Value.ToUpperInvariant();
                    var value = optionMatch.Groups[2].Value.Trim();
                    if (key == "A") current.OptionA = value;
                    if (key == "B") current.OptionB = value;
                    if (key == "C") current.OptionC = value;
                    if (key == "D") current.OptionD = value;
                    continue;
                }

                var answerMatch = Regex.Match(line, @"^(?:Đáp\s*án|Dap\s*an|Answer|Correct)\s*[:：-]\s*(.+)$", RegexOptions.IgnoreCase);
                if (answerMatch.Success)
                {
                    current.CorrectAnswer = NormalizeCorrectAnswer(answerMatch.Groups[1].Value);
                    continue;
                }

                var difficultyMatch = Regex.Match(line, @"^(?:Độ\s*khó|Do\s*kho|Difficulty)\s*[:：-]\s*(.+)$", RegexOptions.IgnoreCase);
                if (difficultyMatch.Success)
                {
                    current.Difficulty = NormalizeDifficulty(difficultyMatch.Groups[1].Value);
                    continue;
                }

                var pointMatch = Regex.Match(line, @"^(?:Điểm|Diem|Points?|Score)\s*[:：-]\s*(\d+)", RegexOptions.IgnoreCase);
                if (pointMatch.Success && int.TryParse(pointMatch.Groups[1].Value, out var pts))
                {
                    current.Points = Math.Clamp(pts, 1, 10);
                    continue;
                }

                var typeMatch = Regex.Match(line, @"^(?:Loại|Loai|Type)\s*[:：-]\s*(.+)$", RegexOptions.IgnoreCase);
                if (typeMatch.Success)
                {
                    current.QuestionType = NormalizeQuestionType(typeMatch.Groups[1].Value);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(current.Text)) current.Text = line;
                else current.Text += " " + line;
            }

            if (current != null) AddValidated(result, current);
            return result;
        }

        private static void AddValidated(List<WordQuestionDto> result, WordQuestionDto question)
        {
            question.Text = question.Text?.Trim() ?? "";
            question.OptionA = question.OptionA?.Trim() ?? "";
            question.OptionB = question.OptionB?.Trim() ?? "";
            question.OptionC = question.OptionC?.Trim() ?? "";
            question.OptionD = question.OptionD?.Trim() ?? "";
            question.CorrectAnswer = NormalizeCorrectAnswer(question.CorrectAnswer);
            question.Difficulty = NormalizeDifficulty(question.Difficulty);
            question.QuestionType = NormalizeQuestionType(question.QuestionType);
            question.Points = Math.Clamp(question.Points <= 0 ? 1 : question.Points, 1, 10);

            var validation = ValidateWordQuestion(question);
            question.IsValid = validation.IsValid;
            question.Error = validation.Error;

            if (!string.IsNullOrWhiteSpace(question.Text)) result.Add(question);
        }

        private static (bool IsValid, string Error) ValidateWordQuestion(WordQuestionDto question)
        {
            if (string.IsNullOrWhiteSpace(question.Text))
                return (false, "Thiếu nội dung câu hỏi");

            var type = NormalizeQuestionType(question.QuestionType);
            if (type == "Trắc nghiệm")
            {
                if (string.IsNullOrWhiteSpace(question.OptionA) || string.IsNullOrWhiteSpace(question.OptionB)
                    || string.IsNullOrWhiteSpace(question.OptionC) || string.IsNullOrWhiteSpace(question.OptionD))
                    return (false, "Câu trắc nghiệm phải có đủ A, B, C, D");

                if (!Regex.IsMatch(NormalizeCorrectAnswer(question.CorrectAnswer), "^[A-D]$"))
                    return (false, "Đáp án trắc nghiệm phải là A, B, C hoặc D");
            }
            else if (type == "Đúng/Sai")
            {
                var ans = NormalizeCorrectAnswer(question.CorrectAnswer);
                if (ans != "Đúng" && ans != "Sai" && ans != "A" && ans != "B")
                    return (false, "Đáp án đúng/sai phải là Đúng hoặc Sai");
            }
            else if (type == "Tự luận" && string.IsNullOrWhiteSpace(question.CorrectAnswer))
            {
                question.CorrectAnswer = "Tự luận";
            }

            return (true, "");
        }

        private static string CleanLine(string value)
        {
            return Regex.Replace(value.Replace('\u00A0', ' '), @"\s+", " ").Trim();
        }

        private static string NormalizeCorrectAnswer(string? value)
        {
            var answer = CleanLine(value ?? "");
            if (string.IsNullOrWhiteSpace(answer)) return "";

            answer = answer.Trim('.', ')', ':', '-', ' ');
            var upper = answer.ToUpperInvariant();
            if (upper.Length >= 1 && "ABCD".Contains(upper[0])) return upper[0].ToString();
            if (Regex.IsMatch(answer, "^(đúng|dung|true)$", RegexOptions.IgnoreCase)) return "Đúng";
            if (Regex.IsMatch(answer, "^(sai|false)$", RegexOptions.IgnoreCase)) return "Sai";
            return answer;
        }

        private static string NormalizeDifficulty(string? value)
        {
            var text = CleanLine(value ?? "").ToLowerInvariant();
            if (text.Contains("khó") || text.Contains("kho") || text.Contains("hard")) return "Khó";
            if (text.Contains("dễ") || text.Contains("de") || text.Contains("easy")) return "Dễ";
            return "Trung bình";
        }

        private static string NormalizeQuestionType(string? value)
        {
            var text = CleanLine(value ?? "").ToLowerInvariant();
            if (text.Contains("tự luận") || text.Contains("tu luan") || text.Contains("essay")) return "Tự luận";
            if (text.Contains("đúng") || text.Contains("dung") || text.Contains("sai") || text.Contains("true") || text.Contains("false")) return "Đúng/Sai";
            return "Trắc nghiệm";
        }

        public IActionResult Grading() { ViewData["Title"] = "Chấm bài thi"; return View(); }

        // Danh sách bài có câu tự luận chờ chấm
        [HttpGet]
        public IActionResult UngradedAttempts(int? examId = null)
        {
            var q = _context.ExamAttempts.Include(a => a.Student).Include(a => a.Exam)
                .Where(a => a.Status == "Submitted");
            if (examId != null) q = q.Where(a => a.ExamId == examId);

            var result = q.Select(a => new
            {
                attemptId = a.Id,
                student = a.Student != null ? a.Student.FullName : "",
                exam = a.Exam != null ? a.Exam.Title : "",
                submitTime = a.SubmitTime
            }).ToList();
            return Json(result);
        }

        [HttpGet]
        public IActionResult SubmissionsForGrading(int? examId = null, string? status = null, string? search = null)
        {
            var q = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Include(a => a.Exam)!
                .ThenInclude(e => e.Subject)
                .Include(a => a.Answers)!
                .ThenInclude(an => an.Question)
                .Where(a => a.SubmitTime != null);

            if (examId.HasValue && examId.Value > 0) q = q.Where(a => a.ExamId == examId.Value);
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                if (status == "pending") q = q.Where(a => a.Status == "Submitted");
                else if (status == "graded") q = q.Where(a => a.Status == "Graded");
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                q = q.Where(a => a.Student != null &&
                    ((a.Student.FullName ?? "").ToLower().Contains(kw) || (a.Student.Username ?? "").ToLower().Contains(kw)));
            }

            var result = q.OrderByDescending(a => a.SubmitTime)
                .Take(200)
                .ToList()
                .Select(a =>
                {
                    var answers = a.Answers ?? new List<Answer>();
                    var essayAnswers = answers.Where(x => x.Question != null && x.Question.QuestionType == "Tự luận").ToList();
                    return new
                    {
                        id = a.Id,
                        attemptId = a.Id,
                        student = a.Student != null ? (string.IsNullOrWhiteSpace(a.Student.FullName) ? a.Student.Username : a.Student.FullName) : "Không xác định",
                        examId = a.ExamId,
                        examName = a.Exam != null ? a.Exam.Title : "Bài thi",
                        subject = a.Exam?.Subject?.SubjectName ?? a.Exam?.Description ?? "",
                        date = (a.SubmitTime ?? a.StartTime).ToString("dd/MM/yyyy HH:mm"),
                        score = a.Score.HasValue ? Math.Round(a.Score.Value, 1).ToString("0.0") : "pending",
                        status = a.Status == "Graded" ? "graded" : "pending",
                        statusText = a.Status == "Graded" ? "Đã chấm" : "Chờ chấm",
                        essayTotal = essayAnswers.Count,
                        essayGraded = essayAnswers.Count(x => x.EssayScore.HasValue),
                        violations = a.ViolationCount
                    };
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public IActionResult AttemptDetail(int attemptId)
        {
            var attempt = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Include(a => a.Exam)!
                .ThenInclude(e => e.Subject)
                .Include(a => a.Answers)!
                .ThenInclude(an => an.Question)
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null) return Json(new { success = false, message = "Không tìm thấy bài làm!" });

            var answers = (attempt.Answers ?? new List<Answer>()).OrderBy(a => a.QuestionId).Select(a => new
            {
                answerId = a.Id,
                questionId = a.QuestionId,
                question = a.Question?.Text ?? "",
                type = a.Question?.QuestionType ?? "",
                maxPoints = a.Question?.Points ?? 1,
                selectedAnswer = a.SelectedAnswer ?? "",
                correctAnswer = a.Question?.CorrectAnswer ?? "",
                optionA = a.Question?.OptionA ?? "",
                optionB = a.Question?.OptionB ?? "",
                optionC = a.Question?.OptionC ?? "",
                optionD = a.Question?.OptionD ?? "",
                isCorrect = a.IsCorrect,
                essayScore = a.EssayScore,
                teacherComment = a.TeacherComment ?? ""
            }).ToList();

            return Json(new
            {
                success = true,
                attempt = new
                {
                    id = attempt.Id,
                    student = attempt.Student != null ? (string.IsNullOrWhiteSpace(attempt.Student.FullName) ? attempt.Student.Username : attempt.Student.FullName) : "Không xác định",
                    exam = attempt.Exam?.Title ?? "Bài thi",
                    subject = attempt.Exam?.Subject?.SubjectName ?? attempt.Exam?.Description ?? "",
                    score = attempt.Score.HasValue ? Math.Round(attempt.Score.Value, 1).ToString("0.0") : "Chờ chấm",
                    status = attempt.Status,
                    submitTime = (attempt.SubmitTime ?? attempt.StartTime).ToString("dd/MM/yyyy HH:mm"),
                    violations = attempt.ViolationCount
                },
                answers
            });
        }

        // R33/R34: chấm điểm tự luận cho từng câu rồi cập nhật tổng điểm (R35)
        [HttpPost]
        public IActionResult GradeEssay(int answerId, double score, string? comment)
        {
            var answer = _context.Answers.Include(a => a.Question).FirstOrDefault(a => a.Id == answerId);
            if (answer == null) return Json(new { success = false, message = "Không tìm thấy câu trả lời!" });

            double max = answer.Question?.Points ?? 10;
            if (score < 0) score = 0;
            if (score > max) score = max; // R34
            answer.EssayScore = score;
            answer.TeacherComment = comment ?? "";
            answer.IsCorrect = score > 0;
            _context.SaveChanges();

            RecomputeAttemptScore(answer.ExamAttemptId);
            return Json(new { success = true, message = "Đã lưu điểm!" });
        }

        private void RecomputeAttemptScore(int attemptId)
        {
            var attempt = _context.ExamAttempts.Include(a => a.Answers)!.ThenInclude(an => an.Question)
                .FirstOrDefault(a => a.Id == attemptId);
            if (attempt == null) return;

            double earned = 0; int total = 0;
            bool allEssayGraded = true;
            foreach (var an in attempt.Answers ?? new List<Answer>())
            {
                int pts = an.Question?.Points ?? 1;
                total += pts;
                if (an.Question?.QuestionType == "Tự luận")
                {
                    if (an.EssayScore == null) allEssayGraded = false;
                    else earned += an.EssayScore.Value;
                }
                else if (an.IsCorrect) earned += pts; // R35
            }

            attempt.Score = total > 0 ? Math.Round(earned * 10.0 / total, 1) : 0;
            if (allEssayGraded) attempt.Status = "Graded";
            _context.SaveChanges();
        }

        public IActionResult Proctoring() { ViewData["Title"] = "Giám sát Anti-cheat"; return View(); }

        public IActionResult Statistics() { ViewData["Title"] = "Thống kê kết quả"; return View(); }

        public class WordQuestionDto
        {
            public string Text { get; set; } = "";
            public string OptionA { get; set; } = "";
            public string OptionB { get; set; } = "";
            public string OptionC { get; set; } = "";
            public string OptionD { get; set; } = "";
            public string CorrectAnswer { get; set; } = "";
            public string Difficulty { get; set; } = "Trung bình";
            public string QuestionType { get; set; } = "Trắc nghiệm";
            public int Points { get; set; } = 1;
            public bool IsValid { get; set; }
            public string Error { get; set; } = "";
        }
    }
}
