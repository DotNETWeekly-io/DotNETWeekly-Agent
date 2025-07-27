using Microsoft.Extensions.Primitives;

using System.Security.Cryptography;
using System.Text;

namespace DotNETWeeklyAgent.Services;

public class SecretTokenValidator : ISecretTokenValidator
{
    public async Task<bool> Validate(HttpContext httpContext, string secretToken)
    {
        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        string? payload = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(payload)) return false;
        if (!httpContext.Request.Headers.TryGetValue("x-hub-signature-256", out StringValues signatureSHA256))
        {
            return false;
        }

        byte[] key = Encoding.UTF8.GetBytes(secretToken);
        using var hmac = new HMACSHA256(key);
        byte[] computedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string computedHexString = Convert.ToHexString(computedSig);
        return string.Equals($"sha256={computedSig}", signatureSHA256.ToString());
    }
}
