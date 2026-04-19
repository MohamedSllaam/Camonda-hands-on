namespace Camonda_hands_on.Models.Catering;

public class StartCateringRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime AgreementStartDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Department { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}
