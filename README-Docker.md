# 🐳 Docker Setup for PlatformFlower API

## 🚀 Quick Start

### Prerequisites
- Docker Desktop installed
- Git

### 1. Clone and Setup
```bash
git clone <your-repo-url>
cd SWP391-BE
```

### 2. Configure Environment
```bash
# Copy template files
cp PlatformFlower/appsettings.Production.template.json PlatformFlower/appsettings.Production.json

# Edit the Production settings with your actual values
# - Database connection string
# - Email settings
# - Cloudinary credentials
```

### 3. Run with Docker Compose
```bash
# Build and start all services
docker-compose up --build

# Or run in background
docker-compose up -d --build
```

### 4. Access the Application
- **API Swagger UI**: http://localhost:5000
- **Health Check**: http://localhost:5000/health
- **Database**: localhost:1433 (sa/PlatformFlower123!)

## 🔧 Docker Services

### API Service (`api`)
- **Port**: 5000 → 8080 (container)
- **Environment**: Production
- **Features**: Swagger UI enabled, Health checks

### Database Service (`sqlserver`)
- **Port**: 1433
- **Database**: Flowershop
- **Credentials**: sa/PlatformFlower123!
- **Persistence**: Docker volume `sqlserver_data`

### Database Initialization (`db-init`)
- **Purpose**: Creates database schema
- **Script**: `PlatformFlower/Scripts/CreateFlowershopDatabase.sql`
- **Runs once**: Container exits after completion

## 📋 Available Commands

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f api
docker-compose logs -f sqlserver

# Stop services
docker-compose down

# Rebuild API
docker-compose build api
docker-compose restart api

# Reset everything
docker-compose down -v
docker-compose up --build
```

## 🔍 Health Checks

- `/health` - General health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

## 🛠️ Development

### File Structure
```
├── docker-compose.yml          # Multi-service orchestration
├── Dockerfile                  # API container definition
├── PlatformFlower/
│   ├── Scripts/
│   │   └── CreateFlowershopDatabase.sql
│   ├── appsettings.json        # Development settings
│   ├── appsettings.Production.template.json
│   └── Program.cs              # Updated for Docker
```

### Configuration Changes
- **Swagger**: Enabled in all environments
- **HTTPS**: Disabled in Production (Docker)
- **Database**: Uses container name `sqlserver`
- **CORS**: Allows all origins for development

## 🔒 Security Notes

- `appsettings.Production.json` is gitignored
- Use template file and add your actual credentials
- Database password should be changed in production
- Email credentials should use app passwords

## 🐛 Troubleshooting

### Container Issues
```bash
# Check container status
docker-compose ps

# View container logs
docker-compose logs <service-name>

# Restart specific service
docker-compose restart <service-name>
```

### Database Issues
```bash
# Connect to database
docker exec -it platformflower-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P PlatformFlower123! -C

# Reinitialize database
docker-compose restart db-init
```

### API Issues
```bash
# Check API health
curl http://localhost:5000/health

# View API logs
docker-compose logs -f api
```
