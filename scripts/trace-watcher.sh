#!/usr/bin/env bash

# Usage: ./trace-watcher.sh <process_name>

PROCESS_NAME="$1"

dotnet tool install --global dotnet-trace

while true; do
    PID=$(dotnet trace ps | grep -E "$PROCESS_NAME(\s|$|\.)" | awk '{ print $1 }')
    if [[ -n "$PID" ]]; then
        echo "Found PID: $PID"
        dotnet trace collect --process-id "$PID" --output "${PROCESS_NAME}_trace.nettrace" --providers "OpenTelemetry-Exporter-OpenTelemetryProtocol"
        break
    fi
    sleep 1
done