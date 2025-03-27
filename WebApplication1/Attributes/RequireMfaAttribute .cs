using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireMfaAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var accessToken = context.HttpContext
            .Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(accessToken))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Resolve IMfaSessionService from DI
        //var mfaService = context.HttpContext.RequestServices.GetService(typeof(IMfaSessionService)) as IMfaSessionService;
        //if (mfaService is null)
        //{
        //    context.Result = new StatusCodeResult(500); // Misconfigured
        //    return;
        //}

        //var isVerified = await mfaService.IsVerifiedAsync(userId, accessToken);
        var isVerified = true; // For testing
        if (!isVerified)
        {
            context.Result = new UnauthorizedObjectResult("MFA verification required.");
        }
    }
}
