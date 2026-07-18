namespace snap_test.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public Rating Rating { get; set; } = new();
        public bool InStock { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class Rating
    {
        public double Rate { get; set; }
        public int Count { get; set; }
    }
}
