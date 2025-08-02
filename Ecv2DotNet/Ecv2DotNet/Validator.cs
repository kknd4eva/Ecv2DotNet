using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System.Text;
using System.Text.Json;

namespace Ecv2DotNet;

/// <summary>
/// ECv2 signature validator implementation
/// </summary>
public class Ecv2Validator : IEcv2Validator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Ecv2Validator> _logger;
    private readonly Ecv2Options _options;

    public Ecv2Validator(HttpClient httpClient, IOptions<Ecv2Options> options, ILogger<Ecv2Validator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.IssuerId))
        {
            throw new ArgumentException("IssuerId must be provided in Ecv2Options", nameof(options));
        }
    }

    /// <summary>
    /// Validates an ECv2 signature from raw JSON string
    /// </summary>
    public async Task<bool> ValidateSignatureAsync(string signatureJson)
    {
        if (string.IsNullOrWhiteSpace(signatureJson))
        {
            _logger.LogError("Signature JSON is null or empty");
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SignaturePayload>(signatureJson);
            if (payload == null)
            {
                _logger.LogError("Failed to deserialize signature payload");
                return false;
            }

            return await ValidateSignatureAsync(payload);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse signature JSON");
            return false;
        }
    }

    /// <summary>
    /// Validates an ECv2 signature from a parsed payload
    /// </summary>
    public async Task<bool> ValidateSignatureAsync(SignaturePayload payload)
    {
        try
        {
            // Step 1: Validate protocol version
            if (payload.ProtocolVersion != _options.Protocol)
            {
                _logger.LogError("Invalid protocol version: {ProtocolVersion}", payload.ProtocolVersion);
                return false;
            }

            // Step 2: Parse and validate signed message
            var signedMessageData = JsonSerializer.Deserialize<SignedMessageData>(payload.SignedMessage);
            if (signedMessageData == null)
            {
                _logger.LogError("Failed to parse signed message");
                return false;
            }

            // Step 3: Validate recipient ID in signed message
            if (!ValidateRecipientId(signedMessageData))
            {
                _logger.LogError("Recipient ID validation failed");
                return false;
            }

            // Step 4: Check if signed message has expired
            if (!IsFutureExpiry(signedMessageData.ExpTimeMillis))
            {
                _logger.LogError("Signed message has expired: {ExpTimeMillis}", signedMessageData.ExpTimeMillis);
                return false;
            }

            // Step 5: Parse intermediate key data
            var intermediateKeyData = JsonSerializer.Deserialize<IntermediateKeyData>(payload.IntermediateSigningKey.SignedKey);
            if (intermediateKeyData == null || string.IsNullOrWhiteSpace(intermediateKeyData.KeyValue))
            {
                _logger.LogError("Failed to parse intermediate key data");
                return false;
            }

            // Step 6: Check if intermediate key has expired
            if (!long.TryParse(intermediateKeyData.KeyExpiration, out long keyExpiration) || !IsFutureExpiry(keyExpiration))
            {
                _logger.LogError("Intermediate key has expired: {KeyExpiration}", intermediateKeyData.KeyExpiration);
                return false;
            }

            // Step 7: Fetch Google public keys
            var googleKeys = await FetchGooglePublicKeysAsync();
            if (googleKeys == null || !googleKeys.Keys.Any())
            {
                _logger.LogError("Failed to fetch Google public keys");
                return false;
            }

            // Step 8: Verify intermediate signature
            if (!VerifyIntermediateSignature(payload, googleKeys))
            {
                _logger.LogError("Intermediate signature verification failed");
                return false;
            }

            // Step 9: Verify message signature
            if (!VerifyMessageSignature(payload, intermediateKeyData))
            {
                _logger.LogError("Message signature verification failed");
                return false;
            }

            _logger.LogInformation("ECv2 signature validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate ECv2 signature");
            return false;
        }
    }

    private bool ValidateRecipientId(SignedMessageData signedMessage)
    {
        try
        {
            // Extract issuer ID from classId (format: "issuerId.CLASS_SUFFIX")
            if (!string.IsNullOrEmpty(signedMessage.ClassId))
            {
                var classIdParts = signedMessage.ClassId.Split('.');
                if (classIdParts.Length > 0 && classIdParts[0] == _options.IssuerId)
                {
                    return true;
                }
            }

            // Extract issuer ID from objectId (format: "issuerId.OBJECT_SUFFIX")
            if (!string.IsNullOrEmpty(signedMessage.ObjectId))
            {
                var objectIdParts = signedMessage.ObjectId.Split('.');
                if (objectIdParts.Length > 0 && objectIdParts[0] == _options.IssuerId)
                {
                    return true;
                }
            }

            _logger.LogError("Recipient ID mismatch. Expected: {IssuerId}, ClassId: {ClassId}, ObjectId: {ObjectId}",
                _options.IssuerId, signedMessage.ClassId, signedMessage.ObjectId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating recipient ID");
            return false;
        }
    }

    private bool IsFutureExpiry(long epochTimeMillis)
    {
        try
        {
            var expiryDate = DateTimeOffset.FromUnixTimeMilliseconds(epochTimeMillis);
            var currentDate = DateTimeOffset.UtcNow;
            
            if (expiryDate <= currentDate)
            {
                _logger.LogError("Expiry date is in the past: {ExpiryDate}", expiryDate);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking expiry time: {EpochTime}", epochTimeMillis);
            return false;
        }
    }

    private async Task<GooglePublicKeysResponse?> FetchGooglePublicKeysAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_options.PublicKeyUrl);
            return JsonSerializer.Deserialize<GooglePublicKeysResponse>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Google public keys from {Url}", _options.PublicKeyUrl);
            return null;
        }
    }

    private bool VerifyIntermediateSignature(SignaturePayload payload, GooglePublicKeysResponse googleKeys)
    {
        try
        {
            // Format: length_of_sender_id || sender_id || length_of_protocol_version || protocol_version || length_of_signed_key || signed_key
            var signedString = CreateSignedStringForIntermediateSignature(payload);

            foreach (var key in googleKeys.Keys)
            {
                foreach (var signature in payload.IntermediateSigningKey.Signatures)
                {
                    if (VerifySignature(key.KeyValue, signedString, Convert.FromBase64String(signature)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying intermediate signature");
            return false;
        }
    }

    private bool VerifyMessageSignature(SignaturePayload payload, IntermediateKeyData intermediateKeyData)
    {
        try
        {
            // Format: length_of_sender_id || sender_id || length_of_recipient_id || recipient_id || length_of_protocolVersion || protocolVersion || length_of_signedMessage || signedMessage
            var signedString = CreateSignedStringForMessageSignature(payload);  
            var signatureBytes = Convert.FromBase64String(payload.Signature);

            return VerifySignature(intermediateKeyData.KeyValue, signedString, signatureBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying message signature");
            return false;
        }
    }

    private byte[] CreateSignedStringForIntermediateSignature(SignaturePayload payload)
    {
        var senderId = _options.SenderId;
        return ConcatenateForSigning(
            senderId,
            payload.ProtocolVersion,
            payload.IntermediateSigningKey.SignedKey
        );
    }

    private byte[] CreateSignedStringForMessageSignature(SignaturePayload payload)
    {
        var senderId = _options.SenderId;
        return ConcatenateForSigning(
            senderId,
            _options.IssuerId,
            payload.ProtocolVersion,
            payload.SignedMessage
        );
    }

    private static byte[] ConcatenateForSigning(params string[] values)
    {
        var result = new List<byte>();
        
        foreach (var value in values)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            var lengthBytes = BitConverter.GetBytes(valueBytes.Length);
            
            result.AddRange(lengthBytes);
            result.AddRange(valueBytes);
        }
        
        return result.ToArray();
    }

    private bool VerifySignature(string publicKeyBase64, byte[] signedData, byte[] signature)
    {
        try
        {
            var keyBytes = Convert.FromBase64String(publicKeyBase64);
            var publicKey = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(keyBytes);

            var signer = new DsaDigestSigner(new ECDsaSigner(), new Sha256Digest());
            signer.Init(false, publicKey);
            signer.BlockUpdate(signedData, 0, signedData.Length);
            
            return signer.VerifySignature(signature);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Signature verification failed");
            return false;
        }
    }
}
