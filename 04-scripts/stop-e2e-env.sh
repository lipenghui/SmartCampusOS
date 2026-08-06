#!/usr/bin/env bash
# 停止 SmartCampusOS 功能测试环境(容器 + 服务进程,含残留子进程)
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$ROOT/04-scripts/logs"

# 1) 按 PID 文件停止 dotnet run 父进程
for svc in gateway identity; do
  if [[ -f "$LOG_DIR/$svc.pid" ]]; then
    pid="$(cat "$LOG_DIR/$svc.pid")"
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
      echo "已停止 $svc 父进程 (pid $pid)"
    fi
    rm -f "$LOG_DIR/$svc.pid"
  fi
done

# 2) 按端口清理残留的服务进程(dotnet run 会 fork 实际 app 子进程)
for port in "${IDENTITY_PORT:-5111}" "${GATEWAY_PORT:-5000}"; do
  while read -r pid; do
    if [[ -n "$pid" && "$pid" != "0" ]]; then
      # 跳过 PID 文件里已处理的父进程(可能已退出)
      taskkill //F //PID "$pid" >/dev/null 2>&1 && echo "已终止端口 $port 的残留进程 (pid $pid)" || true
    fi
  done < <(netstat -ano 2>/dev/null | grep -E "[:.]${port}\b" | grep LISTENING | awk '{print $NF}' | sort -u)
done

# 3) 移除容器
for name in e2e-mysql e2e-redis; do
  if docker ps --format '{{.Names}}' | grep -qx "$name"; then
    docker rm -f "$name" >/dev/null
    echo "已移除容器 $name"
  fi
done

echo "环境已停止"
