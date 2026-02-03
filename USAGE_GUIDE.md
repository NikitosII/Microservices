# Microservices E-Commerce Platform - Usage Guide

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- Node.js (v18 or higher) installed
- Git

### Launch the Platform

#### Windows (PowerShell)
```powershell
.\launch.ps1
```

#### Linux/Mac
```bash
chmod +x launch.sh
./launch.sh
```

The script will:
1. Check if Docker is running
2. Start all microservices using Docker Compose
3. Wait for services to be ready
4. Install frontend dependencies (if needed)
5. Launch the dashboard

## Accessing the Platform

### Dashboard
**URL**: http://localhost:55585

The dashboard provides:
- Real-time health monitoring of all microservices
- System metrics (products, orders, coupons count)
- Auto-refresh every 10 seconds
- Manual refresh button

### Microservices Endpoints

| Service | Port | URL | Description |
|---------|------|-----|-------------|
| Gateway API | 5000 | http://localhost:5000 | API Gateway - routes requests to microservices |
| Identity API | 5001 | http://localhost:5001 | User authentication and authorization |
| Product API | 5002 | http://localhost:5002 | Product catalog management |
| Coupon API | 5003 | http://localhost:5003 | Discount coupons management |
| Shopping Cart API | 5004 | http://localhost:5004 | Shopping cart operations |
| Order API | 5005 | http://localhost:5005 | Order processing |
| Payment API | 5007 | http://localhost:5007 | Payment processing |

### Infrastructure Services

| Service | Port | URL | Credentials |
|---------|------|-----|-------------|
| RabbitMQ Management | 15672 | http://localhost:15672 | admin / admin |
| PostgreSQL | 5432 | localhost:5432 | postgres / postgres |

## Using the Dashboard

### Service Health Cards
Each service card shows:
- ✅ **Healthy**: Service is running and responding correctly
- ⚠️ **Unhealthy**: Service is running but not responding properly
- ❌ **Offline**: Service is not reachable

### Metrics Cards
- **📦 Products**: Total number of products in the catalog
- **🛒 Orders**: Total number of orders placed
- **🎟️ Coupons**: Total number of available coupons
- **⚙️ Services**: Number of healthy services vs total services

### Features
- **Auto-refresh**: Metrics update every 10 seconds automatically
- **Manual Refresh**: Click the "🔄 Refresh Now" button to update immediately
- **Responsive**: Works on desktop, tablet, and mobile devices

## Testing the APIs

### Using the Test File
A pre-configured HTTP test file is available: `test-api.http`

You can use it with VS Code REST Client extension or similar tools.

### Example API Calls

#### 1. Register a User
```bash
curl -X POST http://localhost:5001/api/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "confirmPassword": "Password123!"
  }'
```

#### 2. Get All Products
```bash
curl http://localhost:5002/api/products
```

#### 3. Create a Product
```bash
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sample Product",
    "description": "Product Description",
    "price": 99.99,
    "stock": 100
  }'
```

#### 4. Add Item to Cart
```bash
curl -X POST http://localhost:5004/api/cart \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user123",
    "productId": 1,
    "quantity": 2
  }'
```

#### 5. Create an Order
```bash
curl -X POST http://localhost:5005/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user123",
    "items": [
      {
        "productId": 1,
        "quantity": 2,
        "price": 99.99
      }
    ],
    "couponCode": "SUMMER2024"
  }'
```

## Typical Workflow

### 1. Setup Phase
1. Start the platform using `launch.ps1` or `launch.sh`
2. Open the dashboard at http://localhost:55585
3. Wait for all services to show as "Healthy" (green checkmarks)

### 2. Testing E-Commerce Flow
1. **Register a user** via Identity API
2. **Add products** via Product API
3. **Create coupons** via Coupon API
4. **Add items to cart** via Shopping Cart API
5. **Place an order** via Order API
6. **Process payment** via Payment API

### 3. Monitor via Dashboard
- Watch real-time metrics update as you interact with APIs
- Check service health after each operation
- Use the dashboard to ensure all services remain healthy

## Troubleshooting

### Services show as "Offline"
- Ensure Docker is running
- Check if containers are running: `docker ps`
- Check container logs: `docker-compose logs [service-name]`
- Wait 30-60 seconds for services to fully start

### CORS Errors in Dashboard
The APIs should have CORS configured, but if you see errors:
- Check that microservices are running
- Verify the ports are correct in `docker-compose.yml`
- Check browser console for specific error messages

### Frontend Won't Start
```bash
cd client-app
rm -rf node_modules package-lock.json
npm install
npm run dev
```

### Port Already in Use
If a port is already occupied:
1. Stop the process using that port
2. Or modify ports in `docker-compose.yml` and `client-app/src/App.jsx`

### Database Issues
Reset databases:
```bash
docker-compose down -v  # Remove volumes
docker-compose up -d     # Restart with fresh databases
```

## Stopping the Platform

### Stop Frontend Only
Press `Ctrl+C` in the terminal running the frontend.
Microservices will continue running.

### Stop Everything
```bash
docker-compose down
```

### Stop and Remove All Data
```bash
docker-compose down -v
```

## Development Tips

### View Container Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f product.api
docker-compose logs -f gateway.api
```

### Restart a Service
```bash
docker-compose restart product.api
```

### Rebuild a Service
```bash
docker-compose up -d --build product.api
```

### Access PostgreSQL
```bash
docker exec -it postgres psql -U postgres -d ProductDb
```

### Access RabbitMQ Management
Open http://localhost:15672
- Username: `admin`
- Password: `admin`

## Architecture Overview

```
┌─────────────────┐
│   Dashboard     │ (React + Vite)
│ localhost:55585 │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Gateway API   │ (Port 5000)
└────────┬────────┘
         │
    ┌────┴────┬────────┬──────────┬───────────┐
    ▼         ▼        ▼          ▼           ▼
┌────────┐ ┌────────┐ ┌───────┐ ┌──────┐ ┌─────────┐
│Identity│ │Product │ │Coupon │ │ Cart │ │  Order  │
│  API   │ │  API   │ │  API  │ │ API  │ │   API   │
└────────┘ └────────┘ └───────┘ └──────┘ └─────────┘
    │         │          │         │          │
    └─────────┴──────────┴─────────┴──────────┘
              │                     │
         ┌────▼─────┐          ┌───▼──────┐
         │PostgreSQL│          │ RabbitMQ │
         └──────────┘          └──────────┘
```

## Support

For issues or questions:
1. Check container logs: `docker-compose logs`
2. Verify Docker is running
3. Ensure all ports are available
4. Check the dashboard for service status

## Additional Resources

- **API Tests**: See `test-api.http` for example requests
- **Docker Compose**: See `docker-compose.yml` for service configuration
- **Unit Tests**: Each service has corresponding test projects
