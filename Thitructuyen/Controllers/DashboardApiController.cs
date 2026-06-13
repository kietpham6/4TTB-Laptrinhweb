using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thitructuyen.Data;

namespace Thitructuyen.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/dashboard")]
    public class DashboardApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public DashboardApiController(ApplicationDbContext context) => _context = context;

        private int CurrentUserId()
        {
            var value = User.FindFirst("UserId")?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }

        private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value;

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            try
            {
                var now = DateTime.Now;
                var last7Days = Enumerable.Range(0, 7).Select(i => now.Date.AddDays(-6 + i)).ToList();

                var attempts = _context.ExamAttempts
                    .AsNoTracking()
                    .Include(a => a.Exam)!
                    .ThenInclude(e => e.Subject)
                    .ToList();

                var users = _context.Users.AsNoTracking().ToList();
                var exams = _context.Exams.AsNoTracking().ToList();
                var questionsCount = _context.Questions.AsNoTracking().Count();

                var chart = last7Days.Select(d => new
                {
                    label = d.ToString("dd/MM"),
                    attempts = attempts.Count(a => a.StartTime.Date == d || (a.SubmitTime.HasValue && a.SubmitTime.Value.Date == d)),
                    avgScore = Math.Round(attempts.Where(a => a.SubmitTime.HasValue && a.SubmitTime.Value.Date == d && a.Score.HasValue).Select(a => a.Score!.Value).DefaultIfEmpty(0).Average(), 1)
                }).ToList();

                var roleStats = users
                    .GroupBy(u => string.IsNullOrWhiteSpace(u.Role) ? "Chưa có vai trò" : u.Role)
                    .Select(g => new { role = g.Key, count = g.Count() })
                    .ToList();

                var subjectStats = attempts
                    .Where(a => a.Score.HasValue)
                    .GroupBy(a => a.Exam != null && a.Exam.Subject != null ? a.Exam.Subject.SubjectName : (a.Exam != null ? Safe(a.Exam.Description) : "Chưa phân môn"))
                    .Select(g => new { subject = string.IsNullOrWhiteSpace(g.Key) ? "Chưa phân môn" : g.Key, attempts = g.Count(), avgScore = Math.Round(g.Average(x => x.Score ?? 0), 1) })
                    .OrderByDescending(x => x.attempts)
                    .Take(8)
                    .ToList();

                var latestLogs = new List<object>();
                try
                {
                    latestLogs = _context.ActivityLogs.AsNoTracking()
                        .OrderByDescending(l => l.CreatedAt)
                        .Take(8)
                        .Select(l => new
                        {
                            l.Id,
                            l.Action,
                            l.Detail,
                            l.IpAddress,
                            createdAt = l.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                        })
                        .Cast<object>()
                        .ToList();
                }
                catch
                {
                    latestLogs = attempts.OrderByDescending(a => a.SubmitTime ?? a.StartTime)
                        .Take(8)
                        .Select(a => new
                        {
                            Id = a.Id,
                            Action = "ExamAttempt",
                            Detail = $"Lượt thi #{a.Id} - {a.Exam?.Title ?? "Bài thi"}",
                            a.IpAddress,
                            createdAt = (a.SubmitTime ?? a.StartTime).ToString("dd/MM/yyyy HH:mm")
                        })
                        .Cast<object>()
                        .ToList();
                }

                return Ok(new
                {
                    totalUsers = users.Count,
                    totalStudents = users.Count(u => u.Role == "Student"),
                    totalTeachers = users.Count(u => u.Role == "Teacher"),
                    totalExams = exams.Count,
                    totalQuestions = questionsCount,
                    totalAttempts = attempts.Count,
                    submittedAttempts = attempts.Count(a => a.SubmitTime != null),
                    avgScore = Math.Round(attempts.Where(a => a.Score.HasValue).Select(a => a.Score!.Value).DefaultIfEmpty(0).Average(), 1),
                    violations = attempts.Sum(a => a.ViolationCount),
                    todayAttempts = attempts.Count(a => a.StartTime.Date == now.Date),
                    chart,
                    roleStats,
                    subjectStats,
                    latestLogs
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    totalUsers = 0,
                    totalStudents = 0,
                    totalTeachers = 0,
                    totalExams = 0,
                    totalQuestions = 0,
                    totalAttempts = 0,
                    submittedAttempts = 0,
                    avgScore = 0,
                    violations = 0,
                    todayAttempts = 0,
                    chart = Array.Empty<object>(),
                    roleStats = Array.Empty<object>(),
                    subjectStats = Array.Empty<object>(),
                    latestLogs = new[] { new { Id = 0, Action = "Database", Detail = "Không đọc được dữ liệu database: " + ex.Message, IpAddress = "", createdAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm") } }
                });
            }
        }

        [HttpGet("student")]
        [Authorize(Roles = "Student")]
        public IActionResult StudentDashboard()
        {
            var uid = CurrentUserId();
            var attempts = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Exam)!
                .ThenInclude(e => e.Subject)
                .Where(a => a.StudentId == uid)
                .OrderByDescending(a => a.StartTime)
                .ToList();

            var available = _context.Exams.AsNoTracking().Count(e => e.StartTime <= DateTime.Now && e.EndTime >= DateTime.Now);
            var scores = attempts.Where(a => a.Score.HasValue).Select(a => a.Score!.Value).ToList();
            var chart = attempts.Where(a => a.Score.HasValue).OrderBy(a => a.StartTime).TakeLast(10).Select(a => new
            {
                label = a.Exam != null ? a.Exam.Title : $"Lần thi #{a.Id}",
                score = Math.Round(a.Score ?? 0, 1)
            }).ToList();

            return Ok(new
            {
                availableExams = available,
                totalAttempts = attempts.Count,
                submittedAttempts = attempts.Count(a => a.SubmitTime != null),
                avgScore = scores.Count > 0 ? Math.Round(scores.Average(), 1) : 0,
                bestScore = scores.Count > 0 ? Math.Round(scores.Max(), 1) : 0,
                passed = attempts.Count(a => a.Score.HasValue && a.Exam != null && a.Score.Value >= a.Exam.PassScore),
                violations = attempts.Sum(a => a.ViolationCount),
                chart,
                latest = attempts.Take(6).Select(a => new
                {
                    id = a.Id,
                    exam = a.Exam != null ? a.Exam.Title : "Bài thi",
                    subject = a.Exam != null && a.Exam.Subject != null ? a.Exam.Subject.SubjectName : "",
                    score = a.Score.HasValue ? a.Score.Value.ToString("0.0") : "Chưa chấm",
                    status = a.Status,
                    date = (a.SubmitTime ?? a.StartTime).ToString("dd/MM/yyyy HH:mm")
                }).ToList()
            });
        }

        [HttpGet("student-statistics")]
        [Authorize(Roles = "Student")]
        public IActionResult StudentStatistics()
        {
            var uid = CurrentUserId();
            var attempts = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Exam)!
                .ThenInclude(e => e.Subject)
                .Where(a => a.StudentId == uid && a.Score.HasValue)
                .OrderBy(a => a.StartTime)
                .ToList();

            var totalAttempts = attempts.Count;
            var avgScore = totalAttempts > 0 ? Math.Round(attempts.Average(a => a.Score!.Value), 1) : 0;
            var bestScore = totalAttempts > 0 ? Math.Round(attempts.Max(a => a.Score!.Value), 1) : 0;
            var lowestScore = totalAttempts > 0 ? Math.Round(attempts.Min(a => a.Score!.Value), 1) : 0;
            var passed = attempts.Count(a => a.Exam != null && a.Score!.Value >= a.Exam.PassScore);
            var passRate = totalAttempts > 0 ? Math.Round(passed * 100.0 / totalAttempts, 1) : 0;

            var trend = attempts.TakeLast(10).Select(a => new
            {
                label = (a.SubmitTime ?? a.StartTime).ToString("dd/MM"),
                score = Math.Round(a.Score ?? 0, 1),
                exam = a.Exam != null ? a.Exam.Title : "Bài thi"
            }).ToList();

            var bySubject = attempts
                .GroupBy(a => a.Exam != null && a.Exam.Subject != null ? a.Exam.Subject.SubjectName : (a.Exam != null ? Safe(a.Exam.Description) : "Chưa phân môn"))
                .Select(g => new
                {
                    subject = string.IsNullOrWhiteSpace(g.Key) ? "Chưa phân môn" : g.Key,
                    attempts = g.Count(),
                    avgScore = Math.Round(g.Average(x => x.Score ?? 0), 1),
                    highest = Math.Round(g.Max(x => x.Score ?? 0), 1),
                    lowest = Math.Round(g.Min(x => x.Score ?? 0), 1),
                    passRate = Math.Round(g.Count(x => x.Exam != null && x.Score.HasValue && x.Score.Value >= x.Exam.PassScore) * 100.0 / g.Count(), 1)
                })
                .OrderByDescending(x => x.avgScore)
                .ToList();

            var strengths = bySubject.Take(3).ToList();
            var weaknesses = bySubject.OrderBy(x => x.avgScore).Take(3).ToList();

            return Ok(new { totalAttempts, avgScore, bestScore, lowestScore, passRate, violations = attempts.Sum(a => a.ViolationCount), trend, bySubject, strengths, weaknesses });
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public IActionResult Leaderboard(string? subject = null, string? period = null)
        {
            var q = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Include(a => a.Exam)!
                .ThenInclude(e => e.Subject)
                .Where(a => a.Score.HasValue && a.Student != null && a.Student.Role == "Student");

            if (!string.IsNullOrWhiteSpace(subject) && subject != "all")
            {
                q = q.Where(a => a.Exam != null &&
                    ((a.Exam.Subject != null && a.Exam.Subject.SubjectName == subject) || a.Exam.Description == subject));
            }

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

            var data = q.ToList()
                .GroupBy(a => new { a.StudentId, a.Student!.FullName, a.Student.Username, a.Student.AvatarUrl })
                .Select(g => new
                {
                    studentId = g.Key.StudentId,
                    fullName = string.IsNullOrWhiteSpace(g.Key.FullName) ? g.Key.Username : g.Key.FullName,
                    avatar = string.IsNullOrWhiteSpace(g.Key.AvatarUrl) ? "/Temp/images/avatar/default-avatar.png" : g.Key.AvatarUrl,
                    attempts = g.Count(),
                    avgScore = Math.Round(g.Average(x => x.Score ?? 0), 1),
                    bestScore = Math.Round(g.Max(x => x.Score ?? 0), 1),
                    lowestScore = Math.Round(g.Min(x => x.Score ?? 0), 1),
                    violations = g.Sum(x => x.ViolationCount)
                })
                .OrderByDescending(x => x.avgScore)
                .ThenByDescending(x => x.bestScore)
                .ThenBy(x => x.violations)
                .Take(50)
                .ToList();

            return Ok(data.Select((x, i) => new
            {
                rank = i + 1,
                x.studentId,
                name = x.fullName,
                x.fullName,
                x.avatar,
                exams = x.attempts,
                x.attempts,
                avgScore = x.avgScore,
                highest = x.bestScore,
                bestScore = x.bestScore,
                lowest = x.lowestScore,
                x.violations,
                trend = "stable",
                trendValue = "0"
            }));
        }

        [HttpGet("audit-logs")]
        [Authorize(Roles = "Admin,Teacher")]
        public IActionResult AuditLogs(string? action = null, string? search = null)
        {
            try
            {
                var q = _context.ActivityLogs.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(action)) q = q.Where(l => l.Action == action);
                if (!string.IsNullOrWhiteSpace(search)) q = q.Where(l => l.Detail.Contains(search));
                var logs = q.OrderByDescending(l => l.CreatedAt).Take(200).ToList();
                var userIds = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
                var users = _context.Users.AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName);

                var result = logs.Select(l => new
                {
                    l.Id,
                    l.UserId,
                    userName = l.UserId.HasValue && users.ContainsKey(l.UserId.Value) ? users[l.UserId.Value] : "Không xác định",
                    l.Action,
                    l.Detail,
                    l.IpAddress,
                    createdAt = l.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                }).ToList();
                return Ok(result);
            }
            catch
            {
                return Ok(new List<object>());
            }
        }
    }
}
