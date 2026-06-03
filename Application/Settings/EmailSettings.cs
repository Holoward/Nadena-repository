namespace Application.Settings;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SmtpFromEmail { get; set; } = "noreply@nadena.com";
    public string SmtpFromName { get; set; } = "Nadena";
    public string AdminEmail { get; set; } = string.Empty;
}
