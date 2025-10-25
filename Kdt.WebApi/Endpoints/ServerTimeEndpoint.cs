using FastEndpoints;
using Kdt.Share.Response;

namespace Kdt.WebApi.Endpoints;

public class ServerTimeEndpointSummary : Summary<ServerTimeEndpoint>
{
    public ServerTimeEndpointSummary()
    {
        Summary = "서버 시간";
        Description = "현재 서버 시간을 반환합니다.";
        Response<ServerTimeResponse>(200, "성공적으로 서버 시간을 반환했습니다.");
        Response<ProblemDetails>(400, "문제 발생시"); 
        Response<InternalErrorResponse>(500);
    }
}

public class ServerTimeEndpoint : EndpointWithoutRequest<ServerTimeResponse>
{
    private readonly ILogger<ServerTimeEndpoint> _logger;
    
    public ServerTimeEndpoint(ILogger<ServerTimeEndpoint> logger)
    {
        _logger = logger;
    }
    
    public override void Configure()
    {
        Get("/serverTime");
        AllowAnonymous();
        Summary(new ServerTimeEndpointSummary());
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogDebug("ServerTimeEndpoint called. {Remote}", HttpContext.Connection.RemoteIpAddress);
        Response = new ServerTimeResponse()
        {
            Local = DateTime.Now,
            Utc = DateTime.UtcNow
        };
        await Task.CompletedTask;
    } 
}
