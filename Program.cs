using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ServerInstancingService.Model.Services;
using ServerInstancingService.Model.Services.ServerInstances;
using ServerInstancingService.Model.Services.ServerInstances.AsDocker;
using ServerInstancingService.Model.Services.ServerInstances.AsProcess;
using ServerInstancingService.Model.Services.SessionValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("config.json")
    .AddEnvironmentVariables();

string launcherType = builder.Configuration["GameServer:LauncherType"] ?? "Local";

if (launcherType == "Docker")
    builder.Services.AddSingleton<IServerLauncher, DockerLauncher>();
else
    builder.Services.AddSingleton<IServerLauncher, LocalProcessLauncher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<LoggerService>();
builder.Services.AddSingleton<IPortService, PortService>();
builder.Services.AddSingleton<AllocatorService>();
builder.Services.AddSingleton<InstanceAllocatorService>();

// Проверка игровых сессий выполняется на доверенном бэкэнде AirWarsDatabase:
builder.Services.AddOptions<AuthBackendOptions>()
    .Bind(builder.Configuration.GetSection(AuthBackendOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "AuthBackend:BaseUrl must be an absolute URL.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SessionValidationPath),
        "AuthBackend:SessionValidationPath must not be empty.")
    .Validate(options => options.TimeoutSeconds > 0,
        "AuthBackend:TimeoutSeconds must be positive.")
    .ValidateOnStart();

builder.Services.AddHttpClient<ISessionValidationService, SessionValidationService>(
    (serviceProvider, httpClient) =>
    {
        AuthBackendOptions authBackendOptions = serviceProvider
            .GetRequiredService<IOptions<AuthBackendOptions>>().Value;

        httpClient.BaseAddress = new Uri(authBackendOptions.BaseUrl.TrimEnd('/') + "/");
        httpClient.Timeout = TimeSpan.FromSeconds(authBackendOptions.TimeoutSeconds);
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AllocationLimit", opt =>
    {
        opt.PermitLimit = 1; // Сколько запросов разрешено
        opt.Window = TimeSpan.FromSeconds(5); // За какой период времени
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // Не ставим запросы в очередь, сразу отбиваем лишние
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();

var app = builder.Build();

// Настройка чтения заголовков от Nginx
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRateLimiter();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();