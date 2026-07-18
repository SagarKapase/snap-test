namespace snap_test.Models
{
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string PublishedAt { get; set; } = string.Empty;
        public int Likes { get; set; }
    }
}
