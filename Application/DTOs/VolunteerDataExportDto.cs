namespace Application.DTOs;

public class VolunteerDataExportDto
{
    public int VolunteerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PayPalEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ConsentRecordExportDto> ConsentRecords { get; set; } = new();
    public List<PaymentExportDto> PaymentHistory { get; set; } = new();
    public List<UploadHistoryExportDto> UploadHistory { get; set; } = new();
}

public class ConsentRecordExportDto
{
    public string ConsentType { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class PaymentExportDto
{
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string? TransactionId { get; set; }
}

public class UploadHistoryExportDto
{
    public DateTime UploadedAt { get; set; }
    public int CommentCount { get; set; }
    public string FileName { get; set; } = string.Empty;
}
