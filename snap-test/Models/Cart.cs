namespace snap_test.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string UpdatedAt { get; set; } = string.Empty;
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
