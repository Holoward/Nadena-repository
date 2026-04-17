namespace Application.DTOs;

public class VolunteerDto
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Status { get; set; }
    public string YouTubeAccountAge { get; set; }
    public string CommentCountEstimate { get; set; }
    public string ContentTypes { get; set; }
    public string FileLink { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public string BuyerReference { get; set; }
    public bool PaymentSent { get; set; }
    public string Notes { get; set; }
    public string PayPalEmail { get; set; }
}

public class CreateVolunteerDto
{
    public string UserId { get; set; }
    public string YouTubeAccountAge { get; set; }
    public string CommentCountEstimate { get; set; }
    public string ContentTypes { get; set; }
    public string PayPalEmail { get; set; }
}
