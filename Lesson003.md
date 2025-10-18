# Lesson 003

FastEndpoints 사용법

## .NET 에서 대표적인 RestApi 구현법

### Controllers (ASP.NET Core MVC)

구조

- 컨트롤러 클래스 내에서 메서드로 엔드포인트를 정의하는 전통적인 클래스 기반 접근 방식. Model-View-Controller (MVC) 패턴을 따름.

기능

- 라우팅, 필터, 모델 바인딩, 완전한 OpenAPI 지원 등 내장 기능이 풍부한 생태계.

성능

- 더 많은 오버헤드와 복잡성으로 인해 일반적으로 세 가지 중 가장 성능이 낮음.

장점

- 많은 개발자에게 친숙하며, 확립된 패턴을 가진 복잡한 애플리케이션에 적합하고, 강력한 커뮤니티 지원.

단점

- 많은 엔드포인트를 가진 크고 복잡한 클래스로 이어질 수 있어 유지보수성을 저해할 가능성.

### Minimal APIs

구조

- Program.cs 파일 내에서 직접 또는 확장 메서드를 통해 엔드포인트를 정의하는 가볍고 간결한 방식.

기능

- 내장된 매개변수 바인딩과 인증을 통해 단순성과 성능에 중점.

성능

- Controllers보다 상당한 성능 향상을 제공하며, 가장 빠름.

장점

- 매우 빠르고, 보일러플레이트 코드가 적으며, 소규모 서비스, 마이크로서비스 또는 프로토타입에 이상적.

단점

- 신중한 구조화 없이는 대규모 복잡한 애플리케이션에서 조직화되지 않고 관리하기 어려워질 수 있음.

### FastEndpoints

구조

- 각 클래스가 단일 엔드포인트를 나타내는 구조화된 단일 책임 접근 방식을 제공하는 서드파티 라이브러리.

기능

- 입력에 대한 검증, 타입 안전 요청/응답, 인증 등의 내장 기능과 함께 성능과 구조 사이의 균형을 제공.

성능

- Minimal APIs와 Controllers중간.

장점

- Minimal APIs의 속도와 더 구조화되고 조직화된 개발 경험을 결합하여 유지보수성을 촉진.

단점

- 서드파티 의존성을 도입하므로 일부 프로젝트에서는 고려사항이 될 수 있음.

## [FastEndpoints](https://fast-endpoints.com/docs/get-started#create-project-install-package)

```bash
dotnet add package FastEndpoints
```

```csharp
using FastEndpoints;

var bld = WebApplication.CreateBuilder();
bld.Services.AddFastEndpoints();

var app = bld.Build();
app.UseFastEndpoints();
app.Run();
```

[HelloWorldEndpoint.cs](./Kdt.WebApi/Endpoints/HelloWorldEndpoint.cs)

```csharp
// 이전
app.MapGet("/", () => "Hello World!");
```

```csharp
// 이후
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
```

[엔드포인트 타입](https://fast-endpoints.com/docs/get-started#endpoint-types)
