# Lesson 004

Serolog 사용법

## 로그

애플리케이션이 실행되는 동안 발생하는 이벤트에 대한 기록으로, 프로그램의 동작을 이해하고 문제 해결, 성능 개선 등에 활용됩니다.

로그는 애플리케이션, 서버, 네트워크 등에서 발생하는 오류, 사용자 활동, 데이터베이스 트랜잭션 등의 정보를 시간순으로 기록하며, 다양한 로깅 라이브러리를 통해 관리합니다.

### 로그의 주요 기능

이벤트 기록

- 애플리케이션이 실행되는 동안의 중요한 이벤트를 기록합니다.

문제 해결

- 개발 및 운영 과정에서 발생하는 오류나 비정상적인 동작을 분석하여 문제를 신속하게 파악하고 해결할 수 있습니다.

성능 모니터링

- 애플리케이션의 성능을 추적하고 병목 현상을 발견하는 데 사용됩니다.

보안 감사

- 사용자 접근, 권한 변경 등 보안과 관련된 활동을 추적할 수 있습니다.

### 로그의 종류

시스템 로그

- 운영 체제나 서버에서 발생하는 이벤트(예: 연결 시도, 오류, 구성 변경)를 기록합니다.

애플리케이션 로그

- 애플리케이션 자체에서 발생하는 이벤트(예: 함수 호출, 데이터베이스 트랜잭션)를 기록합니다.

디버그 로그

- 개발자가 개발 중 시스템의 내부 동작을 확인하기 위한 상세한 로그로, 보통 운영 환경에서는 사용되지 않습니다.

정보 로그

- 일반적인 이벤트 정보를 기록합니다.

### .NET 로깅 방법

내장 로깅 API

- .NET은 기본적으로 로깅 기능을 제공하며, Microsoft Learn에서 자세한 내용을 확인할 수 있습니다.

로깅 라이브러리

- log4net과 같은 외부 라이브러리를 사용하여 더 다양한 기능을 활용할 수 있습니다.

로그 수준

- 로그 메시지의 중요도를 나타내는 수준(Severity)을 설정하여, 필요한 로그만 필터링할 수 있습니다. 예를 들어, 'Critical', 'Error', 'Warning', 'Information', 'Debug' 등 다양한 수준이 있습니다.

## .NET 로깅 라이브러리 비교

Serilog, Nlog, log4net은 기능, 성능, 사용 편의성 면에서 차이가 있습니다.

### Serilog

장점

- JSON 형식의 로그를 포함해 다양한 형식으로 출력 가능.
- 파일, 콘솔, Elasticsearch 등 다양한 목적지에 로그 기록 가능.
- 가장 널리 사용되는 라이브러리 중 하나입니다.

단점

- 기능이 많아 학습 곡선이 있을 수 있습니다.

### Nlog

장점

- Log4net보다 설정이 더 쉽습니다.
- 최신 라이브러리로, .NET Core 프레임워크를 잘 지원합니다.

단점

- Log4net처럼 일부 문제점이 발생할 수 있습니다.

### Log4net

장점

- 오래된 만큼 다양한 Appender(사용방법)를 제공합니다.

단점

- 오래되어 업데이트가 잘 되지 않고 문서에 누락된 내용이 많습니다.
- Nlog보다 설정이 복잡할 수 있습니다.

## Serilog 설치 및 사용법

### Serilog 패키지 설치

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Async
dotnet add package Serilog.Enrichers.Context
```

### Serilog 구성 및 설정

[Program.cs](./Kdt.WebApi/Program.cs)

```csharp
...
using Serilog;
...
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    builder.Host.UseSerilog();
    
    builder.Services.AddSerilog((services, lc) =>
    {
        lc.ReadFrom.Configuration(builder.Configuration);
        lc.ReadFrom.Services(services);
    });
    ...
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
```

### Serilog 설정 파일

[appsettings.json](./Kdt.WebApi/appsettings.json)

```json
...
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Sinks.Async",
      "Serilog.Enrichers.Context"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.AspNetCore": "Information",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Async",
        "Args": {
          "Configure": [
            {
              "Name": "Console",
              "Args": {
                "OutputTemplate": "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj} {NewLine}{Exception} TraceId={TraceId} SpanId={SpanId}{NewLine}"
              }
            },
            {
              "Name": "File",
              "Args": {
                "Path": "logs/kdt-web-api-.log",
                "RollingInterval": "Day",
                "RetainedFileCountLimit": 7,
                "SizeLimitBytes": 10485760,
                "RollOnFileSizeLimit": true,
                "OutputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {NewLine}{Exception} TraceId={TraceId} SpanId={SpanId}{NewLine}"
              }
            }
          ],
          "bufferSize": 10000,
          "blockWhenFull": false
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName"
    ],
    "Properties": {
      "Application": "kdt.web.api"
    }
  }
}
```

### Serilog 사용 예제

```csharp
...
public class HelloWorldEndpoint : EndpointWithoutRequest
{
    private readonly ILogger<HelloWorldEndpoint> _logger;
    
    public HelloWorldEndpoint(ILogger<HelloWorldEndpoint> logger)
    {
        _logger = logger;
    }    
... 
    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogDebug("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogTrace("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogInformation("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogWarning("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogError("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        _logger.LogCritical("HelloWorldEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
...
```

```text
[2025-10-19 10:19:50.758 +09:00] [INF] Registered 1 endpoints in 179 milliseconds.  
[2025-10-19 10:19:51.032 +09:00] [INF] Starting application. Development  
[2025-10-19 10:19:51.050 +09:00] [INF] Now listening on: http://localhost:5263  
[2025-10-19 10:19:51.051 +09:00] [INF] Application started. Press Ctrl+C to shut down.  
[2025-10-19 10:19:51.051 +09:00] [INF] Hosting environment: Development  
[2025-10-19 10:19:51.051 +09:00] [INF] Content root path: /Volumes/d/github/kdt/Kdt.WebApi  
[2025-10-19 10:19:51.307 +09:00] [INF] Request starting HTTP/1.1 GET http://localhost:5263/ -  
[2025-10-19 10:19:51.386 +09:00] [INF] Executing endpoint 'HTTP: GET /'  
[2025-10-19 10:19:51.428 +09:00] [INF] HelloWorldEndpoint called. ::1  
[2025-10-19 10:19:51.428 +09:00] [WRN] HelloWorldEndpoint called. ::1  
[2025-10-19 10:19:51.428 +09:00] [ERR] HelloWorldEndpoint called. ::1  
[2025-10-19 10:19:51.428 +09:00] [FTL] HelloWorldEndpoint called. ::1  
[2025-10-19 10:19:51.451 +09:00] [INF] Executed endpoint 'HTTP: GET /'  
[2025-10-19 10:19:51.453 +09:00] [INF] Request finished HTTP/1.1 GET http://localhost:5263/ - 200 null application/json; charset=utf-8 149.5108ms 
[2025-10-19 10:21:44.405 +09:00] [INF] Application is shutting down... 
```
