using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thitructuyen.Data;
using Thitructuyen.Models;
using Microsoft.AspNetCore.Authorization;

namespace Thitructuyen.Controllers
{
    public class ExamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams
                .Where(e => e.StartTime <= DateTime.Now && e.EndTime >= DateTime.Now)
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

            HttpContext.Session.SetInt32("ExamId", id);
            HttpContext.Session.SetInt32("StartTime", (int)DateTime.Now.Ticks);

            return View(exam);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitExam(Dictionary<int, string> answers, int examId)
        {
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            int score = 0;
            foreach (var question in exam.Questions)
            {
                if (answers.ContainsKey(question.Id) && answers[question.Id] == question.CorrectAnswer)
                {
                    score += question.Points;
                }
            }

            ViewBag.Score = score;
            ViewBag.TotalPoints = exam.Questions.Sum(q => q.Points);
            ViewBag.Percentage = (score * 100.0) / ViewBag.TotalPoints;

            return View("Result");
        }

        [HttpPost]
        public IActionResult AutoSave(Dictionary<int, string> answers, int examId)
        {
            // Save to session or database
            HttpContext.Session.SetString("SavedAnswers", System.Text.Json.JsonSerializer.Serialize(answers));
            return Ok(new { success = true });
        }

        [HttpPost]
        public IActionResult LogViolation(int examId, string violationType, DateTime timestamp)
        {
            // Log violation to database
            Console.WriteLine($"Violation: ExamId={examId}, Type={violationType}, Time={timestamp}");
            return Ok(new { success = true });
        }

        public IActionResult Result()
        {
            return View();
        }
    }
}