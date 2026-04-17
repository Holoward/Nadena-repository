using Domain.Common;

namespace Domain.Entities;

public class DatasetPurchase : AuditableBaseEntityGuid
{
    public Guid BuyerId { get; set; }
    public Guid DatasetId { get; set; }
    public string StripeSessionId { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string DownloadUrl { get; set; }
    public DateTime DownloadExpiry { get; set; }
    public bool IsRefunded { get; set; }
    public string PurchaseType { get; set; } = "OneTime";
    public string BillingFrequency { get; set; } = "OneTime";
    public string Status { get; set; } = "Processing";
    public int RecordCount { get; set; }
    public string DataSources { get; set; } = string.Empty;
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int RefreshCount { get; set; }
    public DateTime? NextRefreshDate { get; set; }
    public DateTime? LastRefreshedAt { get; set; }
    public string MetricsHistoryJson { get; set; } = "[]";
}
