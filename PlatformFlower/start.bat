@echo off
echo 🌸 Starting PlatformFlower with Docker...

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not running. Please start Docker Desktop first.
    pause
    exit /b 1
)

echo 📦 Building and starting services...
docker-compose up -d --build

echo ⏳ Waiting for services to be ready...
timeout /t 30 /nobreak >nul

echo 🔍 Checking service health...

REM Check API
curl -f http://localhost:5000/health >nul 2>&1
if errorlevel 1 (
    echo ❌ API is not ready yet, please wait a moment...
) else (
    echo ✅ API is ready
)

echo.
echo 🎉 PlatformFlower is starting up!
echo 📍 API: http://localhost:5000
echo 📍 Swagger: http://localhost:5000/swagger
echo 📍 Health: http://localhost:5000/health
echo.
echo 📊 View logs: docker-compose logs -f
echo 🛑 Stop: docker-compose down
echo.
pause
