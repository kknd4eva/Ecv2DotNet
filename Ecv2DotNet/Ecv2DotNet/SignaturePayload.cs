using System.Text.Json.Serialization;

namespace Ecv2DotNet
{
    /// <summary>
    /// Represents the main signature payload from Google Pay callback
    /// </summary>
    public class SignaturePayload
    {
        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;

        [JsonPropertyName("intermediateSigningKey")]
        public IntermediateSigningKey IntermediateSigningKey { get; set; } = new();

        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = string.Empty;

        [JsonPropertyName("signedMessage")]
        public string SignedMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the intermediate signing key structure
    /// </summary>
    public class IntermediateSigningKey
    {
        [JsonPropertyName("signedKey")]
        public string SignedKey { get; set; } = string.Empty;

        [JsonPropertyName("signatures")]
        public List<string> Signatures { get; set; } = new();
    }

    /// <summary>
    /// Represents the parsed intermediate key data
    /// </summary>
    public class IntermediateKeyData
    {
        [JsonPropertyName("keyValue")]
        public string KeyValue { get; set; } = string.Empty;

        [JsonPropertyName("keyExpiration")]
        public string KeyExpiration { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the signed message data
    /// </summary>
    public class SignedMessageData
    {
        [JsonPropertyName("classId")]
        public string? ClassId { get; set; }

        [JsonPropertyName("objectId")]
        public string? ObjectId { get; set; }

        [JsonPropertyName("eventType")]
        public string? EventType { get; set; }

        [JsonPropertyName("expTimeMillis")]
        public long ExpTimeMillis { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("nonce")]
        public string? Nonce { get; set; }
    }

    /// <summary>
    /// Represents Google's public key response
    /// </summary>
    public class GooglePublicKeysResponse
    {
        [JsonPropertyName("keys")]
        public List<GooglePublicKey> Keys { get; set; } = new();
    }

    /// <summary>
    /// Represents a single Google public key
    /// </summary>
    public class GooglePublicKey
    {
        [JsonPropertyName("keyValue")]
        public string KeyValue { get; set; } = string.Empty;

        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = string.Empty;
    }
}
