# ApiGateway 镜像构建（LLD §2.4：Docker + K8s 部署）
# 多阶段构建：SDK 编译 → ASP.NET 运行时镜像
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先复制项目文件以利用层缓存
COPY Directory.Packages.props* ./
COPY src/ApiGateway/ApiGateway.csproj src/ApiGateway/
RUN dotnet restore src/ApiGateway/ApiGateway.csproj

COPY . .
RUN dotnet publish src/ApiGateway/ApiGateway.csproj -c Release -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
