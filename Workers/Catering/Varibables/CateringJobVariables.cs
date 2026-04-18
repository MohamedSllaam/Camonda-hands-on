namespace Camonda_hands_on.Workers.Catering.Varibables;
using System.Text.Json.Serialization;

public class CateringJobVariables
{
    [JsonPropertyName("createRequestId")]
    public string CreateRequestId { get; set; } = string.Empty;

    [JsonPropertyName("agreementStartDate")]
    public DateTime AgreementStartDate { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }

    [JsonPropertyName("approvedBy")]
    public string? ApprovedBy { get; set; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }

    [JsonPropertyName("withdrawReason")]
    public string? WithdrawReason { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }
}