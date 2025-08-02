using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecv2DotNet;

/// <summary>
/// Interface for ECv2 signature validation
/// </summary>
public interface IEcv2Validator 
{
    /// <summary>
    /// Validates an ECv2 signature from raw JSON string
    /// </summary>
    /// <param name="signatureJson">The raw JSON string containing the signature payload</param>
    /// <returns>True if the signature is valid, false otherwise</returns>
    Task<bool> ValidateSignatureAsync(string signatureJson);

    /// <summary>
    /// Validates an ECv2 signature from a parsed payload
    /// </summary>
    /// <param name="payload">The parsed signature payload</param>
    /// <returns>True if the signature is valid, false otherwise</returns>
    Task<bool> ValidateSignatureAsync(SignaturePayload payload);
}
