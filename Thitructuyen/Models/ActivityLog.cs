namespace Thitructuyen.Models
{
    // Nhật ký hoạt động + log vi phạm khi thi (R31, giám sát thi, audit cho Admin)
    public class ActivityLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;   // Login, Violation, Submit...
        public string Detail { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
