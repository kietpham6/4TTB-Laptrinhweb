using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Thitructuyen.Data;

namespace Thitructuyen.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StudentController(ApplicationDbContext context) => _context = context;

        private int CurrentUserId()
        {
            var value = User.FindFirst("UserId")?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard học sinh";
            return View();
        }

        public IActionResult History()
        {
            ViewData["Title"] = "Lịch sử thi";
            return View();
        }

        public IActionResult Ranking()
        {
            ViewData["Title"] = "Xếp hạng";
            return View();
        }

        public IActionResult Statistics()
        {
            ViewData["Title"] = "Thống kê cá nhân";
            return View();
        }

        public async Task<IActionResult> ResultDetail(int attemptId)
        {
            ViewData["Title"] = "Chi tiết kết quả";
            var uid = CurrentUserId();
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam).ThenInclude(e => e!.Subject)
                .Include(a => a.Student)
                .Include(a => a.Answers!).ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == uid);

            if (attempt == null) return NotFound("Không tìm thấy kết quả bài thi.");
            return View(attempt);
        }

        public async Task<IActionResult> ExportResultPdf(int attemptId)
        {
            var uid = CurrentUserId();
            var attempt = await _context.ExamAttempts
                .Include(a => a.Exam).ThenInclude(e => e!.Subject)
                .Include(a => a.Student)
                .Include(a => a.Answers!).ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == uid);

            if (attempt == null) return NotFound("Không tìm thấy kết quả bài thi.");

            // File HTML in được trực tiếp ra PDF bằng trình duyệt, không phụ thuộc thư viện ngoài.
            // Người dùng mở file -> Ctrl+P -> Save as PDF. Nội dung được thiết kế như phiếu kết quả PDF.
            var html = BuildPrintableResultHtml(attempt);
            var bytes = Encoding.UTF8.GetBytes(html);
            var safeName = $"Ket_qua_bai_thi_{attempt.Id}.html";
            return File(bytes, "text/html; charset=utf-8", safeName);
        }


        private static string H(string? text) => System.Net.WebUtility.HtmlEncode(text ?? string.Empty);

        private static string BuildPrintableResultHtml(Thitructuyen.Models.ExamAttempt attempt)
        {
            var answers = attempt.Answers?.OrderBy(a => a.QuestionId).ToList() ?? new List<Thitructuyen.Models.Answer>();
            var rows = new StringBuilder();
            var index = 1;
            foreach (var a in answers)
            {
                rows.Append($@"<tr>
<td>{index++}</td>
<td>{H(a.Question?.Text)}</td>
<td>{H(a.SelectedAnswer)}</td>
<td>{H(a.Question?.CorrectAnswer)}</td>
<td>{(a.IsCorrect ? "Đúng" : "Sai")}</td>
</tr>");
            }

            return $@"<!doctype html>
<html lang='vi'>
<head>
<meta charset='utf-8'>
<title>Phiếu kết quả bài thi</title>
<style>
body{{font-family:Arial,sans-serif;margin:32px;color:#14233b;background:#fff}}
.header{{display:flex;align-items:center;gap:16px;border-bottom:3px solid #0d6efd;padding-bottom:18px;margin-bottom:22px}}
.logo{{width:74px;height:74px;object-fit:contain}}
h1{{margin:0;font-size:26px;color:#0d3b78}} .muted{{color:#64748b}}
.grid{{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:18px 0}}
.box{{border:1px solid #dbe7f6;border-radius:12px;padding:14px;background:#f8fbff}}
.score{{font-size:38px;font-weight:800;color:#0d6efd}}
table{{width:100%;border-collapse:collapse;margin-top:18px}} th,td{{border:1px solid #dbe7f6;padding:10px;text-align:left;vertical-align:top}} th{{background:#eef5fc;color:#0d3b78}}
.actions{{margin:18px 0}} button{{background:#0d6efd;color:#fff;border:0;border-radius:10px;padding:10px 16px;font-weight:700;cursor:pointer}}
@media print{{.actions{{display:none}} body{{margin:12mm}}}}
</style>
</head>
<body>
<div class='header'><img class='logo' src='/images/main-logo.png'><div><h1>PHIẾU KẾT QUẢ BÀI THI</h1><div class='muted'>Hệ thống Thi Trực Tuyến</div></div></div>
<div class='actions'><button onclick='window.print()'>Xuất / Lưu PDF</button></div>
<div class='grid'>
<div class='box'><b>Học sinh:</b> {H(attempt.Student?.FullName)}<br><b>Tài khoản:</b> {H(attempt.Student?.Username)}<br><b>Email:</b> {H(attempt.Student?.Email)}</div>
<div class='box'><b>Bài thi:</b> {H(attempt.Exam?.Title)}<br><b>Môn học:</b> {H(attempt.Exam?.Subject?.SubjectName)}<br><b>Thời gian nộp:</b> {(attempt.SubmitTime.HasValue ? attempt.SubmitTime.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa nộp")}</div>
</div>
<div class='box'><span class='muted'>Điểm tổng kết</span><div class='score'>{(attempt.Score ?? 0):0.0}/10</div><b>Trạng thái:</b> {H(attempt.Status)} &nbsp; | &nbsp; <b>Số vi phạm:</b> {attempt.ViolationCount}</div>
<h2>Chi tiết câu trả lời</h2>
<table><thead><tr><th>STT</th><th>Câu hỏi</th><th>Đáp án chọn</th><th>Đáp án đúng</th><th>Kết quả</th></tr></thead><tbody>{rows}</tbody></table>
</body></html>";
        }
    }
}
