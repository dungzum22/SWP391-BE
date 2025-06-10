#!/bin/bash

echo "🌸 Starting PlatformFlower with Docker..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker Desktop first."
    exit 1
fi

# Check if ports are available
if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null ; then
    echo "❌ Port 5000 is already in use. Please stop the service using this port."
    exit 1
fi

if lsof -Pi :1433 -sTCP:LISTEN -t >/dev/null ; then
    echo "❌ Port 1433 is already in use. Please stop the service using this port."
    exit 1
fi

echo "📦 Building and starting services..."
docker-compose up -d --build

echo "⏳ Waiting for services to be ready..."
sleep 30

echo "🔍 Checking service health..."

# Check database
if docker exec platformflower-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PlatformFlower123! -Q "SELECT 1" > /dev/null 2>&1; then
    echo "✅ Database is ready"
else
    echo "❌ Database is not ready"
fi

# Check API
if curl -f http://localhost:5000/health > /dev/null 2>&1; then
    echo "✅ API is ready"
else
    echo "❌ API is not ready"
fi

echo ""
echo "🎉 PlatformFlower is starting up!"
echo "📍 API: http://localhost:5000"
echo "📍 Swagger: http://localhost:5000/swagger"
echo "📍 Health: http://localhost:5000/health"
echo ""
echo "📊 View logs: docker-compose logs -f"
echo "🛑 Stop: docker-compose down"
