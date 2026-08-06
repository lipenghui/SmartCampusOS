// SmartCampusOS.AppHost — .NET Aspire 本地编排
// 对应 LLD §2.4 编排选型 + §10.1 开发环境
//
// 约定：
// - 每个服务显式固定 HTTP 端口（无 launchSettings.json 的服务若不显式配置，
//   不会收到 Aspire 的 ASPNETCORE_URLS 注入，将全部默认绑定 5000 导致冲突）；
// - 数据库库名与 LLD §4.1 一致（identity_db / edu_db / ...），并显式注入
//   服务实际读取的强类型配置键（Identity:Database:ConnectionString 等），
//   避免服务回退到 appsettings 默认连接串；
// - ApiGateway 固定 5000，与前端 Web/admin dev 代理默认目标一致（LLD §2.7.3.4）。

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================
// 基础设施容器（LLD §2.2 基础设施层）
// ============================================================

// MySQL：每服务独立数据库（LLD §4.1；资源名仅允许 ASCII 字母/数字/连字符）
var mysqlServer = builder.AddMySql("mysql-main");

var mysqlIdentity = mysqlServer.AddDatabase("identity-db");
var mysqlEdu = mysqlServer.AddDatabase("edu-db");
var mysqlDorm = mysqlServer.AddDatabase("dorm-db");
var mysqlNotice = mysqlServer.AddDatabase("notice-db");
var mysqlData = mysqlServer.AddDatabase("data-db");
var mysqlPush = mysqlServer.AddDatabase("push-db");
var mysqlFile = mysqlServer.AddDatabase("file-db");

// Redis：缓存 / 会话 / 计数（LLD §9）
var redis = builder.AddRedis("redis");

// RabbitMQ：领域事件总线（LLD §7 MassTransit + RabbitMQ）
var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// ============================================================
// 微服务项目（LLD §2.3 服务划分）
// ============================================================

// IdentityService — 认证授权（Common-01~04），HTTP 5111
var identityService = builder.AddProject<Projects.IdentityService_Api>("identity-service")
    .WithHttpEndpoint(port: 5111, name: "http")
    .WithReference(mysqlIdentity)
    .WithEnvironment("Identity:Database:ConnectionString", mysqlIdentity)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mysqlIdentity)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// EduService — 教务（Edu-01~06），HTTP 5112
var eduService = builder.AddProject<Projects.EduService_Api>("edu-service")
    .WithHttpEndpoint(port: 5112, name: "http")
    .WithReference(mysqlEdu)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mysqlEdu)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// DormService — 宿舍后勤（Dorm-01~06），HTTP 5113
var dormService = builder.AddProject<Projects.DormService_Api>("dorm-service")
    .WithHttpEndpoint(port: 5113, name: "http")
    .WithReference(mysqlDorm)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mysqlDorm)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// NoticeService — 通知家校（Noti-01~05），HTTP 5114
var noticeService = builder.AddProject<Projects.NoticeService_Api>("notice-service")
    .WithHttpEndpoint(port: 5114, name: "http")
    .WithReference(mysqlNotice)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mysqlNotice)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// DataService — 数据可视化（Data-01~04），HTTP 5115
var dataService = builder.AddProject<Projects.DataService_Api>("data-service")
    .WithHttpEndpoint(port: 5115, name: "http")
    .WithReference(mysqlData)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mysqlData)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// PushGateway — 推送网关（Common-06），HTTP 5116
var pushGateway = builder.AddProject<Projects.PushGateway_Api>("push-gateway")
    .WithHttpEndpoint(port: 5116, name: "http")
    .WithReference(mysqlPush)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mysqlPush)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// FileService — 文件服务，HTTP 5117
var fileService = builder.AddProject<Projects.FileService_Api>("file-service")
    .WithHttpEndpoint(port: 5117, name: "http")
    .WithReference(mysqlFile)
    .WithReference(redis)
    .WaitFor(mysqlFile)
    .WaitFor(redis);

// ============================================================
// API 网关（LLD §3.1 YARP 反向代理，统一入口），HTTP 5000
// ============================================================

builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithReference(identityService)
    .WithReference(eduService)
    .WithReference(dormService)
    .WithReference(noticeService)
    .WithReference(dataService)
    .WithReference(pushGateway)
    .WithReference(fileService)
    .WithReference(redis)
    .WaitFor(identityService)
    .WaitFor(eduService)
    .WaitFor(dormService)
    .WaitFor(noticeService)
    .WaitFor(dataService)
    .WaitFor(pushGateway)
    .WaitFor(fileService)
    .WaitFor(redis);

builder.Build().Run();
