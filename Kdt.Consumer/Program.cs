using FastEndpoints;
using FastEndpoints.Swagger;
using Kdt.Consumer;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 환경별 설정 파일 추가
var environment = builder.Environment.EnvironmentName;
var environmentFromEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.IsNullOrWhiteSpace(environmentFromEnv) == false)
    environment = environmentFromEnv;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

Log.Logger = new LoggerConfiguration()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .CreateLogger();
try
{
    // OpenTelemetry 환경 변수 설정 (AddServiceDefaults 호출 전에 설정 필요)
    builder.AddOpenTelemetryApplication(Log.Logger);

    // ServiceDefaults 추가 (OpenTelemetry, 헬스 체크, 서비스 디스커버리 등)
    builder.AddServiceDefaults();

    builder.Host.UseSerilog();

    builder.Services.AddSerilog((services, lc) =>
    {
        lc.ReadFrom.Configuration(builder.Configuration);
        lc.ReadFrom.Services(services);
    });

    builder.Services.AddFastEndpoints();
    // Scalar API Reference 및 Swagger 설정
    builder.Services.SwaggerDocument();

    var app = builder.Build();

    app.MapDefaultEndpoints();
    app.UseFastEndpoints();

    if (app.Environment.IsDevelopment())
    {
        // Swagger 및 Scalar API Reference 미들웨어 설정
        app.UseOpenApi(options =>
        {
            options.Path = "/openapi/{documentName}.json";
        });
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle(app.Environment.ApplicationName)
                .WithTheme(ScalarTheme.None)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.RestSharp)
                .WithBundleUrl("https://cdn.jsdelivr.net/npm/@scalar/api-reference@latest/dist/browser/standalone.js");
        });
    }

    Log.Information("Starting application. {Environment}", environment);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
