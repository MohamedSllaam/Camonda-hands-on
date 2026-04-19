namespace Camonda_hands_on.Models.Ordering;

public class Order
{
    public int Id { get; set; }
    public int orderItemId { get; set; }
    public decimal Price { get; set; }
    public string OrderRequestId { get; set; } = Guid.NewGuid().ToString();
    public string OrderStatus { get; set; } = "Pending";
      
}
