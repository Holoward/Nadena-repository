namespace Application.DTOs;

public class VolunteerDataExportDto
{
    public int VolunteerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PayPalEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<WatchEventExportDto> WatchEvents { get; set; } = new();
    public List<ConsentRecordExportDto> ConsentRecords { get; set; } = new();
    public List<PaymentExportDto> PaymentHistory { get; set; } = new();
    public List<UploadHistoryExportDto> UploadHistory { get; set; } = new();
}

public class WatchEventExportDto
{
    public string Category { get; set; } = string.Empty;
    public DateTime WatchedAt { get; set; }
    public int HourOfDay { get; set; }
    public int DayOfWeek { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int SessionId { get; set; }
    public int PositionInSession { get; set; }
    public bool IsRepeat { get; set; }
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
