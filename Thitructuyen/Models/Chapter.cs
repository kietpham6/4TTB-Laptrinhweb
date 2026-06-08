namespace Thitructuyen.Models
{
    public class Chapter
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int Order { get; set; }
        public virtual Subject? Subject { get; set; }
    }
}
