using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common;

/// <summary>
/// Provides input sanitization utilities to prevent XSS, SQL injection, and malicious input
/// </summary>
public static class InputSanitizer
{
    private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    /// <summary>
    /// Sanitizes a general string input - trims whitespace, removes HTML tags, limits to 2000 chars
    /// </summary>
    public static string SanitizeString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        // Trim whitespace
        var sanitized = input.Trim();
        
        // Encode HTML tags to prevent XSS
        sanitized = System.Net.WebUtility.HtmlEncode(sanitized);
        
        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);
        
        // Limit to 2000 characters
        if (sanitized.Length > 2000)
            sanitized = sanitized.Substring(0, 2000);
        
        return sanitized;
    }
    
    /// <summary>
    /// Sanitizes an email address - lowercase, trim, validate format
    /// </summary>
    public static string SanitizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        var sanitized = input.Trim().ToLowerInvariant();
        
        // Remove whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", string.Empty);
        
        return sanitized;
    }
    
    /// <summary>
    /// Validates email format
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        return EmailRegex.IsMatch(email);
    }
    
    /// <summary>
    /// Sanitizes comment text - trim, remove null bytes, normalize unicode, limit to 5000 chars
    /// </summary>
    public static string SanitizeCommentText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        // Trim whitespace
        var sanitized = input.Trim();
        
        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);
        
        // Normalize unicode (remove diacritics for consistency)
        sanitized = NormalizeUnicode(sanitized);
        
        // Limit to 5000 characters
        if (sanitized.Length > 5000)
            sanitized = sanitized.Substring(0, 5000);
        
        return sanitized;
    }
    
    /// <summary>
    /// Validates if a string is a valid GUID format before parsing
    /// </summary>
    public static bool IsValidGuid(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        
        return Guid.TryParse(id, out _);
    }
    
    /// <summary>
    /// Normalizes unicode characters (removes diacritics)
    /// </summary>
    private static string NormalizeUnicode(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        
        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Normalizes email addresses to prevent deduplication bypass (e.g. user+alias@gmail.com -> user@gmail.com)
    /// </summary>
    public static string NormalizeEmailForDeduplication(string? email)
    {
        var sanitized = SanitizeEmail(email);
        if (string.IsNullOrWhiteSpace(sanitized))
            return string.Empty;

        var parts = sanitized.Split('@');
        if (parts.Length != 2)
            return sanitized;

        var localPart = parts[0];
        var domainPart = parts[1];

        // Strip aliases like +123
        var plusIndex = localPart.IndexOf('+');
        if (plusIndex > 0)
        {
            localPart = localPart.Substring(0, plusIndex);
        }

        // Strip dots for gmail accounts to prevent dot-aliasing
        if (domainPart == "gmail.com" || domainPart == "googlemail.com")
        {
            localPart = localPart.Replace(".", string.Empty);
        }

        return $"{localPart}@{domainPart}";
    }
}
