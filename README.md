# Ecv2DotNet

Ecv2DotNet is a .NET library that provides a simple way to verify the integrity of payloads that are signed/encrypted
using the Google ecv2 protocol. Currently it supports `ECv2SigningOnly` protocol, which is used for signing payloads without encryption. 
Intention is to eventually support [`ECv2`](https://developers.google.com/pay/api/android/guides/resources/payment-data-cryptography) protocol as well.

Below is an example of a payload that is signed using the `ECv2SigningOnly` protocol. This has come from the Google Wallet callback API.

```
{
  "signature": "MEUCIQCJi26vl+ak17dsHDbZZnRZxm51duUAPiYLwOIr9rVvAAIgGUfR18gpKTq1+Msav0vPrWvC6x9dDRwWFX/b85+jE1k\u003d",
  "intermediateSigningKey": {
    "signedKey": "{\"keyValue\":\"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEVsEtOdPMaE+DJDzuCJaO7EJXaHor4Kyklp411iwfBa+5TmdbiEWUXzewA79H0PXjdyRMhKBY99+sh056JB75LQ\\u003d\\u003d\",\"keyExpiration\":\"1754778096000\"}",
    "signatures": [
      "MEUCIC29Ju3bt9kklbbA9QAJZW0hh2zecbHDzGo4hF1zRi1zAiEA6e201l1TEl85Row6XHybfDoewIKC4vYpnrlmUT9WbrE\u003d"
    ]
  },
  "protocolVersion": "ECv2SigningOnly",
  "signedMessage": "{\"classId\":\"1388000000022025937.LOYALTY_CLASS_dada6069-0799-44ec-a38d-c482484902e1\",\"objectId\":\"3388000000022025937.LOYALTY_OBJECT_xxxxxxxxxxxxx\",\"eventType\":\"save\",\"expTimeMillis\":1754114831806,\"count\":1,\"nonce\":\"40a8e5af-5b7f-4ea4-b152-63d96858550e\"}"
}
```

## Installation

```bash
dotnet add package Ecv2DotNet
```

## Basic Usage

### 

### 1. Dependency Injection Setup



# NOTES 

using AWS.Lambda.Powertools.Logging;
using Core.Constants;
using Core.Features.Google.Callback;
using Core.Shared.ExternalServices.GoogleWallet.Dtos;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System.Text;
using System.Text.Json;


namespace Core.Shared.ExternalServices.GoogleWallet
{
    public class GooglePayAuthenticationService : IGooglePayAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _issuerId;

        public GooglePayAuthenticationService(HttpClient httpClient, string issuerId)
        {
            _issuerId = issuerId ?? throw new ArgumentNullException(nameof(issuerId));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<bool> IsValidECv2SignatureAsync(CallbackGoogleCardCommand callbackCommand)
        {
            try
            {
                // Step 1: Validate protocol version
                if (callbackCommand.ProtocolVersion != GoogleConstants.Protocol)
                {
                    Logger.LogError($"Invalid protocol version: {callbackCommand.ProtocolVersion}");
                    return false;
                }

                // Step 2: Validate recipient ID in signed message
                if (!ValidateRecipientId(callbackCommand.SignedMessage))
                {
                    Logger.LogError("Recipient ID validation failed");
                    return false;
                }

                var publicKeysResponse = await FetchGooglePublicKeysAsync();

                var result = VerifyCallbackSignatureInternal(callbackCommand, publicKeysResponse);

                return result; 

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process Google public keys for ECv2 signature validation.");
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
                    if (classIdParts.Length > 0 && classIdParts[0] == _issuerId)
                    {
                        return true;
                    }
                }

                // Extract issuer ID from objectId (format: "issuerId.OBJECT_SUFFIX")
                if (!string.IsNullOrEmpty(signedMessage.ObjectId))
                {
                    var objectIdParts = signedMessage.ObjectId.Split('.');
                    if (objectIdParts.Length > 0 && objectIdParts[0] == _issuerId)
                    {
                        return true;
                    }
                }

                Logger.LogError($"Recipient ID mismatch. Expected: {_issuerId}, ClassId: {signedMessage.ClassId}, ObjectId: {signedMessage.ObjectId}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error validating recipient ID: {ex.Message}");
                return false;
            }
        }

        private async Task<GooglePublicKeysResponseDto> FetchGooglePublicKeysAsync()
        {
            var response = await _httpClient.GetStringAsync(GoogleConstants.PublicKeyUrl);
            return JsonSerializer.Deserialize<GooglePublicKeysResponseDto>(response);
        }

        private bool VerifyIntermediateSignature(CallbackGoogleCardCommand callbackCommand, byte[] generatedSignature, GooglePublicKeysResponseDto googleKeys)
        {
            List<bool> results = [];
            foreach (GooglePublicKey key in googleKeys.Keys)
            {
                foreach (string internalSignature in callbackCommand.IntermediateSigningKey.Signatures)
                {
                    results.Add(VerifySignature(key.KeyValue, generatedSignature, Convert.FromBase64String(internalSignature)));
                }
            }

            return results.Any(x => x == true);
        }

        private bool VerifySignature(string key, byte[] generatedSignature, byte[] expectedSignature)
        {
            byte[] keyBytes = Org.BouncyCastle.Utilities.Encoders.Base64.Decode(key);

            ECPublicKeyParameters? signingKey;
            try
            {
                signingKey = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(keyBytes);
            }
            catch (Exception)
            {
                return false;
            }

            var dsaSigner = new DsaDigestSigner(new ECDsaSigner(), new Sha256Digest());
            dsaSigner.Init(false, signingKey);
            dsaSigner.BlockUpdate(generatedSignature, 0, generatedSignature.Length);
            return dsaSigner.VerifySignature(expectedSignature);
        }

        private bool VerifyMessageSignature(CallbackGoogleCardCommand callbackCommand, byte[] generatedSignature)
        {
            byte[] signatureBytes = Convert.FromBase64String(callbackCommand.Signature);
            string? intermediateKey = callbackCommand.IntermediateKeyData.KeyValue;
            if (intermediateKey == null)
                return false;

            return VerifySignature(intermediateKey, generatedSignature, signatureBytes);
        }

        private static byte[] GetLengthRepresentation(string str)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            byte[] bytes = BitConverter.GetBytes(strBytes.Length);

            return bytes;
        }

        private bool VerifyCallbackSignatureInternal(CallbackGoogleCardCommand? callbackCommand, GooglePublicKeysResponseDto googleKeys)
        {
            if (callbackCommand == null)
                return false;

            // format of signedStringForIntermediateSigningKeySignature:
            // length_of_sender_id || sender_id || length_of_protocol_version || protocol_version || length_of_signed_key || signed_key

            string senderId = GoogleConstants.SenderId;

            byte[] signedStringForIntermediateSigningKeySignature =
            [
                .. GetLengthRepresentation(senderId),
                .. Encoding.UTF8.GetBytes(senderId),
                .. GetLengthRepresentation(callbackCommand.ProtocolVersion),
                .. Encoding.UTF8.GetBytes(callbackCommand.ProtocolVersion),
                .. GetLengthRepresentation(callbackCommand.IntermediateSigningKey.SignedKey),
                .. Encoding.UTF8.GetBytes(callbackCommand.IntermediateSigningKey.SignedKey),
            ];

            if (!VerifyIntermediateSignature(callbackCommand, signedStringForIntermediateSigningKeySignature, googleKeys))
                return false;

            if (callbackCommand.IntermediateKeyData.KeyExpiration == null)
                return false;

            if (!IsFutureExpiry(long.Parse(callbackCommand.IntermediateKeyData.KeyExpiration)))
            {
                Logger.LogError("Intermediate key has expired: {KeyExpiration}", callbackCommand.IntermediateKeyData.KeyExpiration);
                return false;
            }

            // format of signedStringForMessageSignature:
            // length_of_sender_id || sender_id || length_of_recipient_id || recipient_id || length_of_protocolVersion || protocolVersion || length_of_signedMessage || signedMessage

            byte[] signedStringForMessageSignature =
            [
                .. GetLengthRepresentation(senderId),
                .. Encoding.UTF8.GetBytes(senderId),
                .. GetLengthRepresentation(_issuerId),
                .. Encoding.UTF8.GetBytes(_issuerId),
                .. GetLengthRepresentation(callbackCommand.ProtocolVersion),
                .. Encoding.UTF8.GetBytes(callbackCommand.ProtocolVersion),
                .. GetLengthRepresentation(callbackCommand.OriginalSignedMessageJson),
                .. Encoding.UTF8.GetBytes(callbackCommand.OriginalSignedMessageJson)
            ];

            if (callbackCommand.SignedMessage?.ExpTimeMillis == null)
                return false;

            // If expired
            if (!IsFutureExpiry(callbackCommand.SignedMessage.ExpTimeMillis))
            {
                Logger.LogError("Signed message has expired: {ExpTimeMillis}", callbackCommand.SignedMessage.ExpTimeMillis);
                return false;
            }

            return VerifyMessageSignature(callbackCommand, signedStringForMessageSignature);
        }

        private bool IsFutureExpiry(long epochTime)
        {

            var expiryDate = DateTimeOffset.FromUnixTimeMilliseconds(epochTime);
            var currentDate = DateTimeOffset.UtcNow;
            if (expiryDate < currentDate)
            {
                Logger.LogError("Expiry date is in the past: {ExpiryDate}", expiryDate);
                return false;
            }
            return true;
        }
    }
}
