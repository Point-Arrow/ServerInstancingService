using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ServerInstancingService.Model.Data;

namespace ServerInstancingService.Model.Services.SessionValidation;

/// <summary>
/// Проверяет сессию игрока через доверенный бэкэнд AirWarsDatabase.
/// SIS сам добавляет серверный API-ключ: клиентский ключ не используется и не принимается.
/// </summary>
public sealed class SessionValidationService : ISessionValidationService
{
    private const string _sessionHeaderName = "X-Session-Id";
    private const string _apiKeyHeaderName = "X-Api-Key";
    private const string _serverApiKeyConfigName = "SERVER_API_KEY";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly AuthBackendOptions _options;
    private readonly LoggerService _loggerService;
    private readonly string _serverApiKey;

    public SessionValidationService(
        HttpClient httpClient,
        IOptions<AuthBackendOptions> options,
        IConfiguration configuration,
        LoggerService loggerService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _loggerService = loggerService;
        _serverApiKey = configuration[_serverApiKeyConfigName] ?? string.Empty;
    }

    public async Task<SessionValidationResult> ValidateAsync(Guid sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_serverApiKey))
        {
            _loggerService.Log(LogType.Error,
                "[Security] SERVER_API_KEY is not configured, session validation is impossible.");
            return SessionValidationResult.NotConfigured();
        }

        using HttpRequestMessage request = new(HttpMethod.Post, _options.SessionValidationPath);
        request.Headers.TryAddWithoutValidation(_sessionHeaderName, sessionToken.ToString("D"));
        request.Headers.TryAddWithoutValidation(_apiKeyHeaderName, _serverApiKey);

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return SessionValidationResult.Invalid();

            if (!response.IsSuccessStatusCode)
            {
                _loggerService.Log(LogType.Warning,
                    $"Session validation backend returned HTTP {(int)response.StatusCode}.");
                return SessionValidationResult.Unavailable();
            }

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            SessionValidationResponseDto? responseDto =
                JsonSerializer.Deserialize<SessionValidationResponseDto>(responseBody, _jsonOptions);

            if (responseDto is null || responseDto.PlayerId <= 0)
            {
                _loggerService.Log(LogType.Warning,
                    "Session validation backend returned an invalid response body.");
                return SessionValidationResult.Unavailable();
            }

            return SessionValidationResult.Valid(responseDto.PlayerId);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _loggerService.Log(LogType.Warning, "Session validation request timed out.");
            return SessionValidationResult.Unavailable();
        }
        catch (HttpRequestException exception)
        {
            _loggerService.Log(LogType.Error, "Session validation request failed: " + exception.Message);
            return SessionValidationResult.Unavailable();
        }
        catch (JsonException exception)
        {
            _loggerService.Log(LogType.Warning,
                "Session validation response is not valid JSON: " + exception.Message);
            return SessionValidationResult.Unavailable();
        }
    }
}
