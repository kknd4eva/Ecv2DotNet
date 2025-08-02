# Ecv2DotNet

Ecv2DotNet is a .NET library that provides a simple way to verify the integrity of payloads that are signed using the Google ECv2SigningOnly protocol. This library is used for validating signatures from Google Pay and Google Wallet callback APIs.

## Features

- **ECv2SigningOnly Protocol Support**: Validates signatures using Google's ECv2SigningOnly protocol
- **Dependency Injection Ready**: Easy integration with .NET's built-in DI container
- **Configurable**: Support for custom issuer IDs and public key URLs
- **Async/Await Support**: Fully asynchronous validation methods
- **Comprehensive Logging**: Detailed logging for debugging and monitoring

## Installation

```bash
dotnet add package Ecv2DotNet
```

## Quick Start

### 1. Dependency Injection Setup

```csharp
using Ecv2DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    // Basic setup with your issuer ID
    services.AddEcv2Validation("1296031581681466393");
    
    // Or with custom public key URL
    services.AddEcv2Validation(
        issuerId: "1296031581681466393",
        publicKeyUrl: "https://custom.google.keys.url"
    );
    
    // Or with additional configuration
    services.AddEcv2Validation("1296031581681466393", options =>
    {
        options.PublicKeyUrl = "https://pay.google.com/gp/m/issuer/keys";
    });
});

var host = builder.Build();
```

### 2. ASP.NET Core Integration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add ECv2 validation services
builder.Services.AddEcv2Validation(
    builder.Configuration["GooglePay:IssuerId"]!
);

var app = builder.Build();
```

### 3. Using the Validator

```csharp
using Ecv2DotNet;

[ApiController]
[Route("api/[controller]")]
public class GooglePayController : ControllerBase
{
    private readonly IEcv2Validator _validator;

    public GooglePayController(IEcv2Validator validator)
    {
        _validator = validator;
    }

    [HttpPost("callback")]
    public async Task<IActionResult> HandleCallback([FromBody] string signatureJson)
    {
        try
        {
            var isValid = await _validator.ValidateSignatureAsync(signatureJson);
            
            if (isValid)
            {
                // Process the valid callback
                return Ok(new { status = "success" });
            }
            else
            {
                return BadRequest(new { error = "Invalid signature" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### 4. Manual Instantiation (without DI)

```csharp
using Ecv2DotNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Create configuration
var options = new Ecv2Options
{
    IssuerId = "1296031581681466393",
    PublicKeyUrl = "https://pay.google.com/gp/m/issuer/keys"
};

// Create HTTP client
using var httpClient = new HttpClient();

// Create logger (optional)
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Ecv2Validator>();

// Create the validator
var validator = new Ecv2Validator(httpClient, Options.Create(options), logger);

// Validate signature
string signatureJson = """
{
  "signature": "XEUCIQCJi26vl+ak17dsHDbZZnRZxm51duUAPiYLwOIr9rVvAAIgGUfR18gpKTq1+Msav0vPrWvC6x9dDRwWFX/b85+jE1k=",
  "intermediateSigningKey": {
    "signedKey": "{\"keyValue\":\"XFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEVsEtOdPMaE+DJDzuCJaO7EJXaHor4Kyklp411iwfBa+5TmdbiEWUXzewA79H0PXjdyRMhKBY99+sh056JB75LQ==\",\"keyExpiration\":\"1754778096000\"}",
    "signatures": [
      "MEUCIC29Ju3bt9kklbbA9QAJZW0hh2zecbHDzGo4hF1zRi1zAiEA6e201l1TEl85Row6XHybfDoewIKC4vYpnrlmUT9WbrE="
    ]
  },
  "protocolVersion": "ECv2SigningOnly",
  "signedMessage": "{\"classId\":\"1296031581681466393.LOYALTY_CLASS_dada6069-0799-44ec-a38d-c482484902e1\",\"objectId\":\"1296031581681466393.LOYALTY_OBJECT_xxxxxxxxxxxxx\",\"eventType\":\"save\",\"expTimeMillis\":1754114831806,\"count\":1,\"nonce\":\"40a8e5af-5b7f-4ea4-b152-63d96858550e\"}"
}
""";

var isValid = await validator.ValidateSignatureAsync(signatureJson);
Console.WriteLine($"Signature is valid: {isValid}");
```

## Configuration

If you include an ILoggerFactory in your DI setup, the library will log detailed information about the validation process. This can help you debug issues with signature validation.

Example: 
```
fail: Ecv2DotNet.Ecv2Validator[0]
      Expiry date is in the past: 08/02/2025 06:07:11 +00:00
fail: Ecv2DotNet.Ecv2Validator[0]
      Signed message has expired: 1754114831806
Signature valid: False
```

### Ecv2Options

```csharp
public class Ecv2Options
{
    /// <summary>
    /// The issuer ID that should match the one in the signed message
    /// Required: Must be set to your Google Pay issuer ID
    /// </summary>
    public string IssuerId { get; set; } = string.Empty;

    /// <summary>
    /// URL to fetch Google's public keys
    /// Default: "https://pay.google.com/gp/m/issuer/keys"
    /// </summary>
    public string PublicKeyUrl { get; set; } = "https://pay.google.com/gp/m/issuer/keys";

    /// <summary>
    /// Sender ID for Google Pay (constant)
    /// Value: "GooglePayPasses"
    /// </summary>
    public string SenderId { get; } = "GooglePayPasses";

    /// <summary>
    /// Protocol version (constant)
    /// Value: "ECv2SigningOnly"
    /// </summary>
    public string Protocol { get; } = "ECv2SigningOnly";
}
```

### appsettings.json Configuration

```json
{
  "GooglePay": {
    "IssuerId": "1296031581681466393",
    "PublicKeyUrl": "https://pay.google.com/gp/m/issuer/keys"
  }
}
```

## API Reference

### IEcv2Validator Interface

```csharp
public interface IEcv2Validator
{
    /// <summary>
    /// Validates an ECv2 signature from raw JSON string
    /// </summary>
    Task<bool> ValidateSignatureAsync(string signatureJson);

    /// <summary>
    /// Validates an ECv2 signature from a parsed payload
    /// </summary>
    Task<bool> ValidateSignatureAsync(SignaturePayload payload);
}
```

### Extension Methods

```csharp
// Basic setup
services.AddEcv2Validation(string issuerId);

// With custom public key URL
services.AddEcv2Validation(string issuerId, string publicKeyUrl);

// With configuration callback
services.AddEcv2Validation(string issuerId, Action<Ecv2Options> configureOptions);
```

## Validation Process

The library performs the following validation steps:

1. **Protocol Version Check**: Ensures the protocol version is "ECv2SigningOnly"
2. **Recipient ID Validation**: Verifies the issuer ID matches in classId or objectId
3. **Expiration Check**: Validates that both the signed message and intermediate key haven't expired
4. **Intermediate Signature Verification**: Verifies the intermediate signing key against Google's public keys
5. **Message Signature Verification**: Verifies the message signature using the intermediate key

## Error Handling

The validator returns `false` for invalid signatures and logs detailed error messages. Common validation failures include:

- Invalid protocol version
- Expired signatures or keys
- Mismatched issuer IDs
- Invalid cryptographic signatures
- Network failures when fetching public keys

## Dependencies

- **.NET 8.0**: Target framework
- **BouncyCastle.Cryptography**: For cryptographic operations
- **Microsoft.Extensions.DependencyInjection**: For DI support
- **Microsoft.Extensions.Http**: For HTTP client factory
- **Microsoft.Extensions.Logging**: For logging support
- **System.Text.Json**: For JSON serialization

## License

MIT License - see LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## Support

For issues and feature requests, please use the GitHub issue tracker.