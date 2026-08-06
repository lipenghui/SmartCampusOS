#!/usr/bin/env bash
# SmartCampusOS 功能测试环境编排脚本(后端 BDD + 前端 E2E 共用)
# 启动:scripts/start-e2e-env.sh   停止:scripts/stop-e2e-env.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$ROOT/scripts/logs"
mkdir -p "$LOG_DIR"

MYSQL_NAME=e2e-mysql
REDIS_NAME=e2e-redis
MYSQL_PORT=${MYSQL_PORT:-3306}
REDIS_PORT=${REDIS_PORT:-6379}
IDENTITY_PORT=${IDENTITY_PORT:-5111}     # 与 ApiGateway appsettings 的 identity 集群地址一致
GATEWAY_PORT=${GATEWAY_PORT:-5000}       # 与前端 vite 默认代理目标一致

wait_health() { # wait_health <url> <timeout_s> <desc>
  local url="$1" timeout="$2" desc="$3" i
  for ((i = 0; i < timeout; i += 2)); do
    if curl -sf "$url" >/dev/null 2>&1; then
      echo "    $desc 就绪 ($url)"
      return 0
    fi
    sleep 2
  done
  echo "    !! $desc 未就绪 ($url)" >&2
  return 1
}

echo "==> 1/4 基础设施容器 (MySQL $MYSQL_PORT / Redis $REDIS_PORT)"
if ! docker ps --format '{{.Names}}' | grep -qx "$MYSQL_NAME"; then
  docker rm -f "$MYSQL_NAME" >/dev/null 2>&1 || true
  docker run -d --name "$MYSQL_NAME" -p "$MYSQL_PORT:3306" \
    -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=identity_db \
    mysql:8.4 >/dev/null
  echo "    mysql:8.4 容器已创建"
else
  echo "    mysql 容器已在运行"
fi
if ! docker ps --format '{{.Names}}' | grep -qx "$REDIS_NAME"; then
  docker rm -f "$REDIS_NAME" >/dev/null 2>&1 || true
  docker run -d --name "$REDIS_NAME" -p "$REDIS_PORT:6379" redis:8-alpine >/dev/null
  echo "    redis:8-alpine 容器已创建"
else
  echo "    redis 容器已在运行"
fi

echo "==> 2/4 等待 MySQL 就绪"
for ((i = 0; i < 90; i += 3)); do
  if docker exec "$MYSQL_NAME" mysqladmin ping -h localhost -uroot -proot --silent >/dev/null 2>&1; then
    echo "    MySQL 就绪"
    break
  fi
  sleep 3
  if ((i >= 87)); then echo "    !! MySQL 未就绪" >&2; exit 1; fi
done

echo "==> 3/4 启动 IdentityService ($IDENTITY_PORT)"
if curl -sf "http://localhost:$IDENTITY_PORT/health" >/dev/null 2>&1 || \
   curl -sf -X POST "http://localhost:$IDENTITY_PORT/api/v1/auth/login" -H 'Content-Type: application/json' -d '{}' >/dev/null 2>&1; then
  echo "    IdentityService 已在运行"
else
  (cd "$ROOT/Backend/Services/IdentityService/src/IdentityService.Api" && \
    nohup dotnet run --no-launch-profile --urls "http://localhost:$IDENTITY_PORT" \
    >"$LOG_DIR/identity.log" 2>&1 & echo $! > "$LOG_DIR/identity.pid")
  sleep 3
  echo "    启动中, 日志: scripts/logs/identity.log"
fi

echo "==> 4/4 启动 ApiGateway ($GATEWAY_PORT)"
if curl -sf "http://localhost:$GATEWAY_PORT/health" >/dev/null 2>&1; then
  echo "    ApiGateway 已在运行"
else
  (cd "$ROOT/Backend/ApiGateway/src/ApiGateway" && \
    Gateway__RateLimit__DefaultRps=10000 \
    nohup dotnet run --no-launch-profile --urls "http://localhost:$GATEWAY_PORT" \
    >"$LOG_DIR/gateway.log" 2>&1 & echo $! > "$LOG_DIR/gateway.pid")
  sleep 3
  echo "    启动中, 日志: scripts/logs/gateway.log"
fi

echo "==> 等待服务就绪"
wait_health "http://localhost:$GATEWAY_PORT/health" 120 "ApiGateway" || exit 1
# 网关就绪不代表 identity 下游可用, 用登录接口(白名单)做端到端探活
for ((i = 0; i < 60; i += 3)); do
  code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "http://localhost:$GATEWAY_PORT/api/v1/auth/login" \
    -H 'Content-Type: application/json' -d '{"grantType":"password","userNo":"admin","password":"Admin@123"}' || true)
  if [[ "$code" == "200" ]]; then echo "    端到端探活通过 (登录 admin → HTTP $code)"; exit 0; fi
  sleep 3
done
echo "    !! 端到端探活失败(最后 HTTP $code)" >&2
echo "    查看日志: scripts/logs/identity.log / scripts/logs/gateway.log" >&2
exit 1
