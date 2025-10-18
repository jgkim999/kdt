# Lesson 002

## 필요 프로그램

[.NET](https://dotnet.microsoft.com/ko-kr/download)

마이크로소프트의 크로스 플랫폼 개발 프레임워크

[Git](https://git-scm.com/downloads)

분산 버전 관리 시스템

[Fork](https://git-fork.com/)

Git용 시각적 클라이언트 도구

[Visual Studio Community](https://visualstudio.microsoft.com/ko/downloads/)

마이크로소프트의 통합 개발 환경 (무료 버전)

[Rider](https://www.jetbrains.com/ko-kr/rider/download/)

JetBrains의 .NET 전용 IDE (비 상업적 용도 무료)

[Insomnia](https://insomnia.rest/)

직관적인 API 테스트 도구

## Minimal API

https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-9.0&tabs=visual-studio

```bash
>dotnet new list

템플릿 이름                                약식 이름                           언어        태그                                                                           
-----------------------------------------  ----------------------------------  ----------  -------------------------------------------------------------------------------
ASP.NET Core 웹 API                        webapi                              [C#],F#     Web/Web API/API/Service                                                        
솔루션 파일                                sln,solution                                    Solution                                                                       
...
```

```bash
>dotnet new sln --name Kdt

"솔루션 파일" 템플릿이 성공적으로 생성되었습니다.

❯ ls or dir
Permissions Size User  Date Modified Git Name
...
.rw-r--r--@  441 jgkim 18 Oct 17:06   -N  Kdt.sln
...
```

```bash
dotnet new web -o Kdt.WebApi
# mac
dotnet sln add ./Kdt.WebApi/Kdt.WebApi.csproj
# windows
dotnet sln add .\Kdt.WebApi\Kdt.WebApi.csproj

dotnet restore
dotnet build Kdt.sln

cd Kdt.WebApi
# mac
dotnet run ./Kdt.WebApi.csproj
# windows
dotnet run .\Kdt.WebApi.csproj
```
