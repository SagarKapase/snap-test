namespace snap_test.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Merchant { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
