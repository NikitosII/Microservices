# Microservices E-Commerce Platform

A microservices-based e-commerce platform built with ASP.NET Core, featuring a real-time monitoring dashboard.

## Quick Start

### Prerequisites
- Docker Desktop
- Node.js (v18+)

### Launch Everything

**Windows (PowerShell):**
```powershell
.\launch.ps1
```

**Linux/Mac:**
```bash
./launch.sh
```

The script will start all microservices and open the dashboard at: **http://localhost:55585**

## Architecture

This solution consists of the following microservices:

- **Gateway.API** (Port 5000) - API Gateway using Ocelot
- **Identity.API** (Port 5001) - Authentication and authorization service
- **Product.API** (Port 5002) - Product management service
- **Coupon.API** (Port 5003) - Coupon and discount management service
- **ShoppingCart.API** (Port 5004) - Shopping cart service
- **Order.API** (Port 5005) - Order management service
- **Payment.API** (Port 5007) - Payment processing service
- **Email.API** - Email notification service
- **EventBus** - Event bus for inter-service communication using RabbitMQ

### Infrastructure
- **PostgreSQL** (Port 5432) - Multiple databases for microservices
- **RabbitMQ** (Port 5672, Management 15672) - Message broker

## Dashboard Features

The React-based dashboard provides:
- ✅ Real-time service health monitoring
- 📊 System metrics (products, orders, coupons)
- 🔄 Auto-refresh every 10 seconds
- 📱 Responsive design
- 🎨 Modern UI with status indicators

## Project Structure

```
MicroservicesECommerce/
├── src/
│   ├── Common/EventBus/
│   ├── Gateway/Gateway.API/
│   ├── Identity/Identity.API/
│   └── Microservices/
│       ├── Product.API/
│       ├── Coupon.API/
│       ├── ShoppingCart.API/
│       ├── Order.API/
│       ├── Email.API/
│       └── Payment.API/
├── client-app/                    # React dashboard
├── docker-compose.yml             # Docker services configuration
├── launch.ps1                     # Windows launch script
├── launch.sh                      # Linux/Mac launch script
└── USAGE_GUIDE.md                # Detailed usage instructions
```

## Documentation

- **[USAGE_GUIDE.md](USAGE_GUIDE.md)** - Complete usage guide with API examples
- **[test-api.http](test-api.http)** - API endpoint tests for REST Client

## Quick Commands

```bash
# Start everything
docker-compose up -d

# View logs
docker-compose logs -f

# Stop everything
docker-compose down

# Stop and remove data
docker-compose down -v

# Start frontend only (if microservices are running)
cd client-app && npm run dev
```

## Service Endpoints

| Service | URL |
|---------|-----|
| Dashboard | http://localhost:55585 |
| Gateway | http://localhost:5000 |
| Identity | http://localhost:5001 |
| Product | http://localhost:5002 |
| Coupon | http://localhost:5003 |
| Shopping Cart | http://localhost:5004 |
| Order | http://localhost:5005 |
| Payment | http://localhost:5007 |
| RabbitMQ Management | http://localhost:15672 (admin/admin) |

