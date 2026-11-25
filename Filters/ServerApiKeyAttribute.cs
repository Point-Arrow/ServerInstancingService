using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace ServerInstancingService.Filters;

/// <summary>
/// Требует доверенный серверный API-ключ из конфигурации (SERVER_API_KEY).
/// Клиентский ключ не принимается: он есть у игрока и секретом не является.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiKeyRequired : Attribute, IAsyncAuthorizationFilter
{
    private const string _apiKeyHeaderName = "X-Api-Key";
    private const string _serverApiKeyConfigName = "SERVER_API_KEY";

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        HttpRequest request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(_apiKeyHeaderName, out StringValues extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("API key is missing.");
            return Task.CompletedTask;
        }

        IConfiguration configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        string? expectedApiKey = configuration[_serverApiKeyConfigName];

        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            context.Result = new ObjectResult("Server API key is not configured on the server.")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return Task.CompletedTask;
        }

        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        byte[] actualBytes = Encoding.UTF8.GetBytes(extractedApiKey.ToString());

        if (expectedBytes.Length != actualBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            context.Result = new UnauthorizedObjectResult("Invalid API key.");
        }

        return Task.CompletedTask;
    }
}
