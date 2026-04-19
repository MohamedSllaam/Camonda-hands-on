namespace Camonda_hands_on.Models.Ordering
{
    public class CreateOrderRequest
    {
        public string OrderRequestId { get; set; } = Guid.NewGuid().ToString();
        public int OrderItemId { get; set; }
        public decimal Price { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}
