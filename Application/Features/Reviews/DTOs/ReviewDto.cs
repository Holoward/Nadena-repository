namespace Application.Features.Reviews.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int DatasetId { get; set; }
    public Guid BuyerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
