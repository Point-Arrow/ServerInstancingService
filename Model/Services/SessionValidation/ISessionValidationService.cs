namespace ServerInstancingService.Model.Services.SessionValidation;

/// <summary>
/// Проверяет игровые сессии через доверенный бэкэнд AirWarsDatabase.
/// </summary>
public interface ISessionValidationService
{
    /// <summary>
    /// Проверяет токен сессии. Не продлевает сессию: за продление отвечает heartbeat клиента.
    /// </summary>
    Task<SessionValidationResult> ValidateAsync(Guid sessionToken, CancellationToken cancellationToken = default);
}
