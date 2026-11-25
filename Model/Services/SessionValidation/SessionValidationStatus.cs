namespace ServerInstancingService.Model.Services.SessionValidation;

/// <summary>Результат проверки игровой сессии на доверенном бэкэнде.</summary>
public enum SessionValidationStatus
{
    /// <summary>Сессия существует и не истекла.</summary>
    Valid = 0,

    /// <summary>Сессии нет, она истекла или токен некорректен.</summary>
    Invalid = 1,

    /// <summary>Бэкэнд проверки сессий недоступен или ответил ошибкой.</summary>
    Unavailable = 2,

    /// <summary>На этом сервисе не настроен SERVER_API_KEY.</summary>
    NotConfigured = 3,
}
