#!/usr/bin/env bash

# Usage: ./trace-watcher.sh <process_name>

PROCESS_NAME="$1"

dotnet tool install --global dotnet-trace

while true; do
    PID=$(dotnet trace ps | awk -v name="$PROCESS_NAME" '$0 ~ name { print $1; exit }')
    if [[ -n "$PID" ]]; then
        echo "Found PID: $PID"
		# View with chrome://tracing
        dotnet trace collect --process-id "$PID" --format Chromium --output "${PROCESS_NAME}_trace.json"
        break
    fi
    sleep 1
done