using Kdt.WebApi.Configs;
using OpenTelemetry;
using OpenTelemetry.Resources;

namespace Kdt.WebApi;

public static class OpenTelemetryInitializer
{
    public static void AddOpenTelemetryApplication(this WebApplicationBuilder appBuilder, Serilog.ILogger logger)
    {
        var openTelemetryConfig = appBuilder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConfig>();
        if (openTelemetryConfig is null)
            throw new NullReferenceException();
        appBuilder.Services.Configure<OpenTelemetryConfig>(appBuilder.Configuration.GetSection("OpenTelemetry"));

        logger.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", openTelemetryConfig.Endpoint);
        
        var openTelemetryBuilder = appBuilder.Services.AddOpenTelemetry();

        // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? openTelemetryConfig.Endpoint;
        logger.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", otlpEndpoint);

        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? openTelemetryConfig.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? openTelemetryConfig.ServiceVersion;
        var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? openTelemetryConfig.ServiceNamespace;
        var deploymentEnvironment = Environment.GetEnvironmentVariable("OTEL_DEPLOYMENT_ENVIRONMENT") ?? openTelemetryConfig.DeploymentEnvironment;

        // OpenTelemetry 리소스 설정
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
        
        openTelemetryBuilder.UseOtlpExporter(
            OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
            new Uri(otlpEndpoint));
    }
}
