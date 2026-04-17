using Microsoft.AspNetCore.Identity;

namespace Persistence.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Data Contributor, Data Client, Admin
    public int? VolunteerId { get; set; }
    public int? BuyerId { get; set; }
    public string PayPalEmail { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool CompanyVerified { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? LastPasswordChangedAt { get; set; }
}
