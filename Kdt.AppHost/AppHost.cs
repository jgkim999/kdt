var builder = DistributedApplication.CreateBuilder(args);

// MySQL 추가 (기본 포트 3306)
var mysqlPassword = builder.AddParameter("mysql-password");
var mysql = builder.AddMySql("mysql", password: mysqlPassword)
    .WithPhpMyAdmin()
    .AddDatabase("kdt-db");

// Valkey (Redis 호환) 추가 (기본 포트 6379)
var valkey = builder.AddValkey("valkey");

// RabbitMQ 추가 (기본 포트 5672, 관리 UI 15672)
var rabbitmqUser = builder.AddParameter("rabbitmq-user");
var rabbitmqPassword = builder.AddParameter("rabbitmq-password");
var rabbitmq = builder.AddRabbitMQ("rabbitmq", userName: rabbitmqUser, password: rabbitmqPassword)
    .WithManagementPlugin();

// Kdt.WebApi 프로젝트 추가 및 리소스 참조
builder.AddProject<Projects.Kdt_WebApi>("kdt-webapi")
    .WithExternalHttpEndpoints()
    .WithReference(mysql)
    .WithReference(valkey)
    .WithReference(rabbitmq);

builder.Build().Run();
