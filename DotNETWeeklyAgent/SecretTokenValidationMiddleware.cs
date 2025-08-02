using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;

using Microsoft.Extensions.Options;

namespace DotNETWeeklyAgent;

public class SecretTokenValidationMiddleware : IMiddleware
{
    private static string[] _SecretTokenPaths = ["/issue/event", "/milestone/event"];
    private readonly ISecretTokenValidator _secretTokenValidator;

    private readonly string _secretToken;

    public SecretTokenValidationMiddleware(ISecretTokenValidator secretTokenValidator, IOptions<GithubOptions> githubOptions)
    {
        _secretTokenValidator = secretTokenValidator;
        _secretToken = githubOptions.Value.SecretToken;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Request.EnableBuffering();
#if DEBUG
        if (_SecretTokenPaths.Contains(context.Request.Path.Value) && !await _secretTokenValidator.Validate(context, _secretToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
#endif
        await next(context);
    }
}
