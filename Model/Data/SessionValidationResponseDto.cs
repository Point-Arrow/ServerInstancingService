namespace ServerInstancingService.Model.Data;

/// <summary>Ответ AirWarsDatabase на запрос проверки сессии.</summary>
public sealed class SessionValidationResponseDto
{
    public int PlayerId { get; set; }
}
