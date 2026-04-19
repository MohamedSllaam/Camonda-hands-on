using System.Text.Json.Serialization;

namespace Camonda_hands_on.Workers.Ordering.Variables;

public class OrderingJobVariables
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("orderItemId")]
    public int orderItemId { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("orderRequestId")]
    public string OrderRequestId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("timeInMinutes")]
    public int TimeInMinutes { get; set; } = 5;

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = "Pending";

    [JsonPropertyName("isEnough")]
    public bool IsEnough { get; set; } 






}
