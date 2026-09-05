using System.ComponentModel.DataAnnotations;

namespace UserService.Application.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 signing key. Must never be hardcoded — set via user-secrets locally or
    /// the Jwt__Key environment variable. At least 32 characters so the key has enough
    /// entropy for HS256.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
