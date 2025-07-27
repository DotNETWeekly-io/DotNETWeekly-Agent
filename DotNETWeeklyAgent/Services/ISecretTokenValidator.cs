namespace DotNETWeeklyAgent.Services;

public interface ISecretTokenValidator
{
    Task<bool> Validate(HttpContext httpContext, string secretToken);
}
