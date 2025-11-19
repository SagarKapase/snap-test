namespace snap_test.Models
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
