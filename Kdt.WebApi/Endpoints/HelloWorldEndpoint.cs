using FastEndpoints;

namespace Kdt.WebApi.Endpoints;

public class HelloWorldEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        Response = "Hello World!";
        await Task.CompletedTask;
    }
}
