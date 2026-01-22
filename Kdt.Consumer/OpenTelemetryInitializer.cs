using Kdt.Consumer.Configs;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace Kdt.Consumer;

public static class OpenTelemetryInitializer
{
    public static void AddOpenTelemetryApplication(this WebApplicationBuilder appBuilder, Serilog.ILogger logger)
    {
        var openTelemetryConfig = appBuilder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConfig>();
        if (openTelemetryConfig is null)
            throw new NullReferenceException();
        appBuilder.Services.Configure<OpenTelemetryConfig>(appBuilder.Configuration.GetSection("OpenTelemetry"));

        // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? openTelemetryConfig.Endpoint;
        logger.Information("OpenTelemetry Endpoint: {OpenTelemetryEndpoint}", otlpEndpoint);

        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? openTelemetryConfig.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? openTelemetryConfig.ServiceVersion;
        var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? openTelemetryConfig.ServiceNamespace;
        var deploymentEnvironment = Environment.GetEnvironmentVariable("OTEL_DEPLOYMENT_ENVIRONMENT") ?? openTelemetryConfig.DeploymentEnvironment;

        // OTEL_EXPORTER_OTLP_ENDPOINT 환경 변수 설정 (ServiceDefaults에서 사용)
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint);
        }

        // OpenTelemetry 리소스 속성 추가 (ServiceDefaults의 설정에 추가됨)
        var openTelemetryBuilder = appBuilder.Services.AddOpenTelemetry();
        openTelemetryBuilder.ConfigureResource(resource =>
        {
            resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: openTelemetryConfig.ServiceInstanceId);
            resource.AddAttributes(new Dictionary<string, object>
            {
                ["service.namespace"] = serviceNamespace,
                ["deployment.environment"] = deploymentEnvironment,
                ["host.name"] = Environment.MachineName,
            });
        });

        // 참고: UseOtlpExporter는 ServiceDefaults에서 이미 호출되므로 여기서 호출하지 않음
    }
}
