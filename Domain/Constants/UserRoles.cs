namespace Domain.Constants;

public static class UserRoles
{
    public const string Admin = "Admin";

    // New public-facing roles
    public const string DataContributor = "Data Contributor";
    public const string DataClient = "Data Client";

    // Legacy roles (accepted for backward compatibility during transition)
    public const string VolunteerLegacy = "Volunteer";
    public const string BuyerLegacy = "Buyer";

    public static bool IsSelfRegisterable(string role)
        => string.Equals(Normalize(role), DataContributor, StringComparison.Ordinal)
           || string.Equals(Normalize(role), DataClient, StringComparison.Ordinal);

    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return string.Empty;
        }

        if (role.Equals(VolunteerLegacy, StringComparison.OrdinalIgnoreCase) ||
            role.Equals(DataContributor, StringComparison.OrdinalIgnoreCase) ||
            role.Equals("DataContributor", StringComparison.OrdinalIgnoreCase))
        {
            return DataContributor;
        }

        if (role.Equals(BuyerLegacy, StringComparison.OrdinalIgnoreCase) ||
            role.Equals(DataClient, StringComparison.OrdinalIgnoreCase) ||
            role.Equals("DataClient", StringComparison.OrdinalIgnoreCase))
        {
            return DataClient;
        }

        if (role.Equals(Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        return role.Trim();
    }
}

