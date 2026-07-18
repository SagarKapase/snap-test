namespace snap_test.Models
{
    public class Todo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
    }
}
