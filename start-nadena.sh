#!/bin/bash
PROJECT_DIR="/home/JDVD/Escritorio/NADENA"
CLIENT_APP="$PROJECT_DIR/WebApi/ClientApp"

echo "Killing existing processes..."
pkill -f dotnet 2>/dev/null
pkill -f react-scripts 2>/dev/null
pkill -f "serve -s build" 2>/dev/null
sleep 2

echo "Running migrations..."
cd "$PROJECT_DIR"
dotnet ef database update --project Persistence --startup-project WebApi

echo "Building frontend..."
cd "$CLIENT_APP"
NODE_OPTIONS=--openssl-legacy-provider npm run build

echo "Starting frontend on :44391..."
npx serve -s build -l 44391 &
sleep 2

echo "Starting API on :5000..."
cd "$PROJECT_DIR"
ASPNETCORE_ENVIRONMENT=Development NADENA_DISABLE_SPA=true dotnet run --project WebApi --no-launch-profile &

echo ""
echo "Frontend : http://localhost:44391"
echo "API      : http://localhost:5000"
echo "Swagger  : http://localhost:5000/swagger"
echo ""
echo "Press Ctrl+C to stop."
trap "pkill -f dotnet; pkill -f 'serve -s build'; exit 0" SIGINT SIGTERM
wait
