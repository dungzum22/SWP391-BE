# 🐳 PlatformFlower Docker Setup

Hướng dẫn chạy PlatformFlower với Docker (Backend + Database)

## 📋 Yêu cầu

- Docker Desktop
- Docker Compose
- 4GB RAM trống
- Port 5000 và 1433 không bị sử dụng

## 🚀 Cách chạy

### **1. Clone repository:**
```bash
git clone https://github.com/dungzum22/SWP391-BE
cd SWP391-BE/PlatformFlower
```

### **2. Chạy với Docker Compose:**
```bash
# Chạy tất cả services (database + backend)
docker-compose up -d

# Xem logs
docker-compose logs -f

# Dừng services
docker-compose down
```

### **3. Truy cập ứng dụng:**
- **API:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health
- **Database:** localhost:1433 (sa/PlatformFlower123!)

## 📦 Services

### **1. SQL Server Database (`sqlserver`)**
- **Image:** mcr.microsoft.com/mssql/server:2022-latest
- **Port:** 1433
- **Username:** sa
- **Password:** PlatformFlower123!
- **Database:** Flowershop

### **2. Database Initialization (`db-init`)**
- Tự động chạy script `CreateFlowershopDatabase.sql`
- Tạo tất cả bảng và schema
- Bao gồm trường reset password

### **3. .NET API (`api`)**
- **Port:** 5000
- **Environment:** Production
- **Health Check:** /health endpoint

## 🔧 Cấu hình

### **Environment Variables:**
```yaml
# Database
SA_PASSWORD=PlatformFlower123!

# API
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Data Source=sqlserver;Initial Catalog=Flowershop;User ID=sa;Password=PlatformFlower123!;Encrypt=True;Trust Server Certificate=True
```

### **Volumes:**
- `sqlserver_data`: Lưu trữ database persistent
- `./Scripts`: Mount database scripts

### **Networks:**
- `platformflower-network`: Internal network cho services

## 🛠️ Commands hữu ích

### **Xem trạng thái:**
```bash
docker-compose ps
```

### **Xem logs:**
```bash
# Tất cả services
docker-compose logs

# Chỉ API
docker-compose logs api

# Chỉ Database
docker-compose logs sqlserver
```

### **Restart services:**
```bash
# Restart tất cả
docker-compose restart

# Restart chỉ API
docker-compose restart api
```

### **Rebuild và chạy lại:**
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### **Xóa tất cả (bao gồm data):**
```bash
docker-compose down -v
docker system prune -a
```

## 🔍 Troubleshooting

### **1. Port đã được sử dụng:**
```bash
# Kiểm tra port
netstat -an | findstr :5000
netstat -an | findstr :1433

# Thay đổi port trong docker-compose.yml
ports:
  - "5001:8080"  # Thay vì 5000:8080
```

### **2. Database không khởi tạo:**
```bash
# Kiểm tra logs database
docker-compose logs sqlserver
docker-compose logs db-init

# Chạy lại database init
docker-compose restart db-init
```

### **3. API không kết nối database:**
```bash
# Kiểm tra connection string
docker-compose logs api

# Test connection
docker exec -it platformflower-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PlatformFlower123! -Q "SELECT 1"
```

### **4. Build lỗi:**
```bash
# Clean build
docker-compose build --no-cache api

# Kiểm tra Dockerfile
docker build -t platformflower-api .
```

## 📊 Health Checks

### **Database Health:**
```bash
curl http://localhost:5000/health
```

### **Manual Database Check:**
```bash
docker exec -it platformflower-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P PlatformFlower123! -Q "SELECT name FROM sys.databases"
```

## 🔐 Security Notes

- **Đổi password mặc định** trong production
- **Sử dụng secrets** thay vì hardcode password
- **Cấu hình firewall** cho ports
- **Enable SSL** cho production

## 📝 Development

### **Chạy chỉ database:**
```bash
docker-compose up sqlserver db-init -d
```

### **Chạy API local với database Docker:**
```bash
# Chạy database
docker-compose up sqlserver db-init -d

# Chạy API local
dotnet run
```

## 🎯 Production Deployment

### **1. Sử dụng external database:**
```yaml
# Bỏ sqlserver service
# Cập nhật connection string
environment:
  - ConnectionStrings__DefaultConnection=Data Source=your-prod-server;...
```

### **2. Sử dụng Docker secrets:**
```yaml
secrets:
  db_password:
    file: ./secrets/db_password.txt
```

### **3. Load balancing:**
```yaml
deploy:
  replicas: 3
```

Chúc bạn thành công! 🌸
