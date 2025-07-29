using System.Security.Cryptography;
using System.Text;

namespace DotNETWeeklyAgent.Services;

public class SecretTokenValidator : ISecretTokenValidator
{
    public async Task<bool> Validate(HttpContext httpContext, string secretToken)
    {
        httpContext.Request.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        string? payload = await reader.ReadToEndAsync();
        httpContext.Request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(payload)) return false;
        string? githubSignature = httpContext.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(githubSignature))
        {
            return false;
        }

        string signatureSHA256 = githubSignature.Replace("sha256=", string.Empty, StringComparison.OrdinalIgnoreCase);

        byte[] secretBytes = Encoding.UTF8.GetBytes(secretToken);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        byte[] computedHash = hmac.ComputeHash(payloadBytes);
        string computedHexString = Convert.ToHexString(computedHash).Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        return string.Equals(computedHexString, signatureSHA256, StringComparison.OrdinalIgnoreCase);
    }
}
