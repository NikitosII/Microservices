# Quick Start Guide

## 1. Launch the Platform

### Windows
```powershell
.\launch.ps1
```

### Linux/Mac
```bash
./launch.sh
```

## 2. Access the Dashboard

Open your browser and go to: **http://localhost:55585**

The dashboard will show:
- ✅ Service health status for all 7 microservices
- 📊 Real-time metrics (products, orders, coupons count)
- 🔄 Auto-refresh every 10 seconds

## 3. Wait for Services to Start

All service cards should show green checkmarks (✅ Healthy).
This typically takes 30-60 seconds on first launch.

## 4. Test the APIs

### Example: Create a Product

```bash
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "description": "A sample product",
    "price": 29.99,
    "stock": 50
  }'
```

### Example: Get All Products

```bash
curl http://localhost:5002/api/products
```

Check the dashboard - the Products count should increase!

## 5. Explore All Services

| Service | Port | Try This |
|---------|------|----------|
| Gateway | 5000 | `curl http://localhost:5000/health` |
| Identity | 5001 | Register a user (see USAGE_GUIDE.md) |
| Product | 5002 | `curl http://localhost:5002/api/products` |
| Coupon | 5003 | `curl http://localhost:5003/api/coupons` |
| Shopping Cart | 5004 | Add items to cart |
| Order | 5005 | `curl http://localhost:5005/api/orders` |
| Payment | 5007 | Process payments |

## 6. Monitor with Dashboard

Watch the metrics update in real-time as you:
- Create products
- Add items to cart
- Place orders
- Apply coupons

## Stopping

### Stop Frontend Only
Press `Ctrl+C` in the terminal

### Stop Everything
```bash
docker-compose down
```

## Troubleshooting

### Services show "Offline"?
- Wait 60 seconds - services are still starting
- Check Docker is running: `docker ps`
- View logs: `docker-compose logs`

### Port already in use?
- Stop conflicting services
- Or change ports in `docker-compose.yml`

### Need to reset everything?
```bash
docker-compose down -v  # Remove all data
.\launch.ps1            # Start fresh
```

## Next Steps

📖 Read **USAGE_GUIDE.md** for:
- Detailed API examples
- Complete e-commerce workflow
- Advanced troubleshooting
- Development tips

🧪 Use **test-api.http** for:
- Pre-configured API requests
- Quick testing in VS Code

---

**That's it! You're ready to go! 🚀**
