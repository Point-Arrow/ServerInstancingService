using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using ServerInstancingService.Model.Services.SessionValidation;

namespace ServerInstancingService.Filters;

/// <summary>
/// Требует валидную игровую сессию в заголовке X-Session-Id.
/// Валидность подтверждает доверенный бэкэнд AirWarsDatabase, куда SIS
/// сам добавляет SERVER_API_KEY. Клиентский API-ключ не используется.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionRequiredAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string SessionHeaderName = "X-Session-Id";

    /// <summary>Ключ HttpContext.Items, под которым лежит проверенный playerId.</summary>
    public const string PlayerIdItemKey = "SessionPlayerId";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        HttpRequest request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(SessionHeaderName, out StringValues extractedToken) ||
            string.IsNullOrWhiteSpace(extractedToken.ToString()))
        {
            context.Result = new UnauthorizedObjectResult("Session token is missing.");
            return;
        }

        if (!Guid.TryParse(extractedToken.ToString(), out Guid sessionToken))
        {
            context.Result = new UnauthorizedObjectResult("Session token is invalid.");
            return;
        }

        ISessionValidationService sessionValidationService =
            context.HttpContext.RequestServices.GetRequiredService<ISessionValidationService>();

        SessionValidationResult validationResult = await sessionValidationService
            .ValidateAsync(sessionToken, context.HttpContext.RequestAborted);

        switch (validationResult.Status)
        {
            case SessionValidationStatus.Valid:
                context.HttpContext.Items[PlayerIdItemKey] = validationResult.PlayerId;
                return;
            case SessionValidationStatus.NotConfigured:
                context.Result = new ObjectResult("Session validation is not configured on the server.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                return;
            case SessionValidationStatus.Unavailable:
                context.Result = new ObjectResult("Session validation service is unavailable.")
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                return;
            default:
                context.Result = new UnauthorizedObjectResult("Session is invalid or expired.");
                return;
        }
    }
}
