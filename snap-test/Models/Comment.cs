namespace snap_test.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
