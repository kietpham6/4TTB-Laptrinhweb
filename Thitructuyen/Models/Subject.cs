namespace Thitructuyen.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string SubjectCode { get; set; } = string.Empty; // TOAN101
        public string SubjectName { get; set; } = string.Empty; // Toán cao cấp
        public string Description { get; set; } = string.Empty;
        public int Credits { get; set; } // Số tín chỉ
        public string Department { get; set; } = string.Empty; // Khoa
        public bool IsActive { get; set; } = true;
    }
}
