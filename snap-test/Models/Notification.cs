namespace snap_test.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool Read { get; set; }
        public string Link { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
