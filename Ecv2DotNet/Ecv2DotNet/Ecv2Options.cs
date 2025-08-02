namespace Ecv2DotNet;

/// <summary>
/// Configuration options for ECv2 signature validation
/// </summary>
public class Ecv2Options
{
    /// <summary>
    /// The issuer ID that should match the one in the signed message
    /// </summary>
    public string IssuerId { get; set; } = string.Empty;

    /// <summary>
    /// URL to fetch Google's public keys. Defaults to Google Pay's official endpoint.
    /// </summary>
    public string PublicKeyUrl { get; set; } = "https://pay.google.com/gp/m/issuer/keys";

    /// <summary>
    /// Sender ID for Google Pay. This should remain constant.
    /// </summary>
    public string SenderId { get; } = "GooglePayPasses";

    /// <summary>
    /// Protocol version. This should remain constant.
    /// </summary>
    public string Protocol { get; } = "ECv2SigningOnly";
}