# CLAUDE.md

이 파일은 Claude Code (claude.ai/code)가 이 저장소에서 작업할 때 가이드를 제공합니다.

## 개요

이 저장소는 .NET 10.0을 사용한 최신 ASP.NET Core 개발을 시연하는 한국 개발자 교육(KDT) 저장소입니다. 로깅(Serilog), 분산 추적(OpenTelemetry), API 문서화(Scalar)를 포함한 완전한 관측 가능성 스택을 보여줍니다. 전통적인 MVC 컨트롤러 대신 구조화되고 고성능의 REST API 접근 방식인 FastEndpoints를 사용합니다.

## 프로젝트 구조

솔루션은 네 개의 프로젝트로 구성됩니다:

- **Kdt.WebApi**: FastEndpoints를 사용하는 메인 웹 API 애플리케이션
- **Kdt.ServiceDefaults**: 공유 Aspire 서비스 기본값 (OpenTelemetry, 헬스 체크, 서비스 디스커버리, 복원력)
- **Kdt.AppHost**: 로컬 개발 환경 오케스트레이션을 위한 .NET Aspire AppHost
- **Kdt.Share**: 공유 모델 및 DTO (.NET Standard 2.1 대상으로 광범위한 호환성)

## 일반적인 명령어

### 빌드 및 실행

```bash
# 전체 솔루션 빌드
dotnet build

# WebApi 프로젝트 직접 실행
dotnet run --project Kdt.WebApi

# Aspire AppHost를 통한 실행 (개발 시 권장)
dotnet run --project Kdt.AppHost

# 패키지 복원
dotnet restore
```

### 테스트 및 개발

```bash
# 핫 리로드를 위한 감시 모드
dotnet watch --project Kdt.WebApi

# 빌드 산출물 정리
dotnet clean
```

### Docker 인프라

`docker-compose` 디렉터리에 포괄적인 관측 가능성 스택이 포함되어 있습니다. 자세한 내용은 `docker-compose/CLAUDE.md`를 참조하세요.

```bash
# 모든 인프라 서비스 시작
cd docker-compose
docker-compose up -d

# 특정 서비스 로그 보기
docker-compose logs -f [service-name]

# 모든 서비스 중지
docker-compose down
```

## 아키텍처

### 엔드포인트 패턴 (FastEndpoints)

각 엔드포인트는 FastEndpoints 기본 타입을 상속하는 단일 책임 클래스입니다:
- `EndpointWithoutRequest`: 요청 본문이 없는 엔드포인트용 (일반적으로 GET 엔드포인트)
- `Endpoint<TRequest>`: 요청 본문이 있는 엔드포인트용
- `Endpoint<TRequest, TResponse>`: 완전한 요청/응답 타입 지정용

엔드포인트는 `Kdt.WebApi/Endpoints/`에 위치하며 시작 시 자동으로 검색됩니다.

예제 구조:
```csharp
public class MyEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/my-route");
        AllowAnonymous(); // 또는 인증 요구사항
        Summary(new MyEndpointSummary());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // 구현
    }
}
```

### 관측 가능성 설정

#### Serilog
`appsettings.json`을 통해 세 가지 싱크로 구성됩니다:
- Console: TraceId/SpanId가 포함된 포맷된 출력
- File: `logs/` 디렉터리의 일일 롤링 로그 (7일 보관, 10MB 크기 제한)
- OpenTelemetry: `http://localhost:4317`의 OTLP 수집기로 내보내기

모든 싱크는 `Serilog.Sinks.Async`를 통해 비동기적으로 실행됩니다.

#### OpenTelemetry
두 가지 구성 계층:
1. **ServiceDefaults** (`Kdt.ServiceDefaults/Extensions.cs`): 메트릭, 추적 및 로깅 익스포터가 포함된 표준 Aspire 텔레메트리
2. **애플리케이션별** (`Kdt.WebApi/OpenTelemetryInitializer.cs`): 서비스 이름, 버전, 네임스페이스 및 배포 환경을 포함한 사용자 정의 리소스 속성

`appsettings.json`의 `OpenTelemetry` 섹션을 통한 구성. 환경 변수 오버라이드 지원:
- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_SERVICE_NAME`
- `OTEL_SERVICE_VERSION`
- `OTEL_SERVICE_NAMESPACE`
- `OTEL_DEPLOYMENT_ENVIRONMENT`

#### Service Defaults 통합
`Program.cs`의 `builder.AddServiceDefaults()` 호출은 다음을 추가합니다:
- OTLP 익스포터와 함께 OpenTelemetry (메트릭, 추적, 로그)
- `/health` 및 `/alive`의 헬스 체크 (개발 환경만)
- 마이크로서비스 통신을 위한 서비스 디스커버리
- 표준 재시도/타임아웃 정책이 포함된 HTTP 클라이언트 복원력

### API 문서화

대화형 API 문서화를 위해 Scalar 사용:
- 개발 모드에서만 사용 가능
- `/scalar/v1` (또는 Scalar UI가 있는 루트 경로)에서 접근 가능
- OpenAPI 사양은 `/openapi/v1.json`에 위치
- 엔드포인트 검색을 위해 FastEndpoints.Swagger 통합 사용

### 구성 파일

환경별 구성 계층화:
1. `appsettings.json` (기본 구성)
2. `appsettings.{Environment}.json` (환경 오버라이드)
3. 환경 변수 (최우선 순위)

환경은 `ASPNETCORE_ENVIRONMENT` 변수로 결정됩니다 (빌더 환경이 기본값).

## 개발 노트

### 새 엔드포인트 추가

1. `Kdt.WebApi/Endpoints/`에 새 클래스 생성
2. 적절한 FastEndpoints 기본 클래스 상속
3. 라우팅 및 보안을 위해 `Configure()` 오버라이드
4. 구현을 위해 `HandleAsync()` 오버라이드
5. OpenAPI 문서화를 위해 선택적으로 `Summary` 클래스 생성
6. FastEndpoints가 엔드포인트를 자동 검색 및 등록

### 로깅 모범 사례

코드베이스는 Serilog를 사용한 구조화된 로깅을 사용합니다. 관련 컨텍스트 속성을 포함하세요:
```csharp
_logger.LogInformation("Action performed. {PropertyName}", value);
```

TraceId 및 SpanId는 분산 추적과의 상관관계를 위해 모든 로그 출력에 자동으로 포함됩니다.

### 대상 프레임워크

`Kdt.Share`를 제외한 프로젝트는 .NET 10.0(`net10.0`)을 대상으로 하며, `Kdt.Share`는 광범위한 호환성을 위해 .NET Standard 2.1을 대상으로 합니다.

### 종속성

주요 패키지:
- FastEndpoints 7.1.1 (REST API 프레임워크)
- OpenTelemetry 1.14.0 (관측 가능성)
- Serilog.AspNetCore 10.0.0 (로깅)
- Scalar.AspNetCore 2.12.5 (API 문서화)
- Aspire.Hosting.AppHost 13.1.0 (로컬 오케스트레이션)

## 레슨 구조

저장소에는 다음을 다루는 레슨 파일(Lesson001.md ~ Lesson006.md)이 포함되어 있습니다:
- Lesson 001: 필수 프로그램 설치
- Lesson 002: ASP.NET Core 기본 개념
- Lesson 003: FastEndpoints 소개 및 Controllers/Minimal APIs와의 비교
- Lesson 004: Serilog 구성
- Lesson 005: OpenTelemetry 설정
- Lesson 006: Scalar API 문서화

이러한 레슨은 코드베이스의 아키텍처 결정에 대한 컨텍스트를 제공합니다.