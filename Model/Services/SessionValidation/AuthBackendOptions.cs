namespace ServerInstancingService.Model.Services.SessionValidation;

/// <summary>
/// Настройки внутреннего бэкэнда AirWarsDatabase, который хранит сессии игроков.
/// </summary>
public sealed class AuthBackendOptions
{
    public const string SectionName = "AuthBackend";

    /// <summary>Адрес внутреннего бэкэнда, например http://127.0.0.1:5001.</summary>
    public string BaseUrl { get; set; } = "https://127.0.0.1:5001";

    /// <summary>Путь эндпоинта проверки сессии на бэкэнде.</summary>
    public string SessionValidationPath { get; set; } = "/Db/Profile/Session/Validate";

    /// <summary>Таймаут запроса проверки сессии в секундах.</summary>
    public int TimeoutSeconds { get; set; } = 5;
}
