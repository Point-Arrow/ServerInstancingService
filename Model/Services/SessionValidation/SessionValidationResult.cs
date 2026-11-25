namespace ServerInstancingService.Model.Services.SessionValidation;

/// <summary>Результат проверки сессии: статус и идентификатор игрока при успехе.</summary>
public sealed record SessionValidationResult(SessionValidationStatus Status, int? PlayerId)
{
    public bool IsValid => Status == SessionValidationStatus.Valid;

    public static SessionValidationResult Valid(int playerId) =>
        new(SessionValidationStatus.Valid, playerId);

    public static SessionValidationResult Invalid() =>
        new(SessionValidationStatus.Invalid, null);

    public static SessionValidationResult Unavailable() =>
        new(SessionValidationStatus.Unavailable, null);

    public static SessionValidationResult NotConfigured() =>
        new(SessionValidationStatus.NotConfigured, null);
}
