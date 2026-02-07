# Microservices E-Commerce Platform - Usage Guide

This guide provides detailed instructions for testing and interacting with all microservices in the platform.

## Table of Contents
- [Quick Start](#quick-start)
- [Checking Service Health](#checking-service-health)
- [Using Swagger UI](#using-swagger-ui)
- [Authentication & Authorization](#authentication--authorization)
- [API Reference](#api-reference)
- [Unit Tests](#unit-tests)
- [Troubleshooting](#troubleshooting)
- [Service Endpoints Quick Reference](#service-endpoints-quick-reference)

---

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- Node.js (v18 or higher) installed

### Launch the Platform

**Windows (PowerShell):**
```powershell
.\launch.ps1
```

**Linux/Mac:**
```bash
chmod +x launch.sh
./launch.sh
```

The script will:
1. Check if Docker is running
2. Start all microservices using Docker Compose
3. Wait for services to be ready
4. Install frontend dependencies (if needed)
5. Launch the dashboard at http://localhost:55585

---

## Checking Service Health

### Method 1: Dashboard (Recommended)
Open **http://localhost:55585** in your browser to see:
- Real-time health status of all 7 microservices
- Green checkmark = Healthy
- Red X = Offline or Unhealthy
- Auto-refresh every 10 seconds

### Method 2: Health Endpoints
Each service exposes a `/api/health` endpoint:

| Service | Health Check URL |
|---------|-----------------|
| Gateway | http://localhost:5000/api/health |
| Identity | http://localhost:5001/api/health |
| Product | http://localhost:5002/api/health |
| Coupon | http://localhost:5003/api/health |
| Shopping Cart | http://localhost:5004/api/health |
| Order | http://localhost:5005/api/health |
| Payment | http://localhost:5007/api/health |

### Method 3: Docker Commands
```bash
docker ps                        # View all running containers
docker logs product.api          # View logs for a specific service
docker-compose logs -f           # Follow logs in real-time
```

---

## Using Swagger UI

Each microservice has built-in Swagger documentation for easy API testing:

| Service | Swagger URL |
|---------|-------------|
| Gateway | http://localhost:5000/swagger |
| Identity | http://localhost:5001/swagger |
| Product | http://localhost:5002/swagger |
| Coupon | http://localhost:5003/swagger |
| Shopping Cart | http://localhost:5004/swagger |
| Order | http://localhost:5005/swagger |
| Payment | http://localhost:5007/swagger |

Open any Swagger URL to view endpoints, see models, and test APIs directly in the browser.

---

## Authentication & Authorization

### Overview

The platform uses **Duende IdentityServer** with JWT Bearer tokens for authentication and authorization. Many endpoints require a valid JWT token, and some endpoints (like creating/updating/deleting products) require admin privileges.

### Default Credentials

A default admin account is automatically created when the Identity service starts:

| Email | Password | Role |
|-------|----------|------|
| `admin@example.com` | `Admin@123` | Admin |

**Note:** This account has full admin privileges and can access all protected endpoints.

### Important information
<details>
<summary><strong>Registering New Users</strong> (click to expand)</summary>

To create a new user account:

**Using Swagger UI:**
1. Open http://localhost:5001/swagger
2. Expand `POST /api/account/register`
3. Click "Try it out"
4. Enter user details:
```json
{
  "email": "user@example.com",
  "password": "User@123",
  "firstName": "John",
  "lastName": "Doe"
}
```
5. Click "Execute"

**Using curl:**
```bash
curl -X POST http://localhost:5001/api/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "User@123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Using PowerShell:**
```powershell
$body = @{
    email = "user@example.com"
    password = "User@123"
    firstName = "John"
    lastName = "Doe"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5001/api/account/register" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

**Password Requirements:**
- At least 6 characters
- Must contain at least one uppercase letter
- Must contain at least one number
- Must contain at least one special character

</details>

<details>
<summary><strong>Getting JWT Tokens</strong> (click to expand)</summary>

#### Method 1: Using Swagger UI

This is the easiest way to get and use JWT tokens for testing:

1. **Open any protected API's Swagger page** (e.g., http://localhost:5002/swagger for Product API)

2. **Click the "Authorize" button** (lock icon) at the top right of the Swagger page

3. **Get a token from IdentityServer:**
   - Open http://localhost:5001/connect/token in a new tab
   - Use the following form data:
     - `grant_type`: `password`
     - `client_id`: `swagger-client`
     - `username`: `admin@example.com` (or any registered user email)
     - `password`: `Admin@123` (or the user's password)
     - `scope`: `openid profile email roles product.api order.api cart.api payment.api`

4. **OR use curl/PowerShell** (see methods below) to get the token

5. **Copy the `access_token` value** from the response (it's a long string starting with `eyJ...`)

6. **Return to the Swagger page** and paste the token into the "Value" field in the authorization dialog

7. **Click "Authorize"** - you should see "Authorized" with a lock icon

8. **Now all API calls** made through this Swagger page will include the JWT token automatically

#### Method 2: Using PowerShell

**For Admin User:**
```powershell
$tokenResponse = Invoke-RestMethod -Uri "http://localhost:5001/connect/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type = "password"
        client_id = "swagger-client"
        username = "admin@example.com"
        password = "Admin@123"
        scope = "openid profile email roles product.api order.api cart.api payment.api"
    }

# Extract the token
$token = $tokenResponse.access_token
Write-Host "Access Token: $token"

# Use the token in API calls
$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

# Example: Create a product (admin only)
$productData = @{
    name = "Laptop Pro"
    description = "High-performance laptop"
    price = 1299.99
    stock = 50
    category = "Electronics"
    imageUrl = "https://example.com/laptop.jpg"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5002/api/products" `
    -Method Post `
    -Headers $headers `
    -Body $productData
```

**For Regular User:**
```powershell
$tokenResponse = Invoke-RestMethod -Uri "http://localhost:5001/connect/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type = "password"
        client_id = "swagger-client"
        username = "user@example.com"
        password = "User@123"
        scope = "openid profile email roles cart.api order.api"
    }

$token = $tokenResponse.access_token
```

#### Method 3: Using curl

**For Admin User:**
```bash
# Get token
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=swagger-client&username=admin@example.com&password=Admin@123&scope=openid profile email roles product.api order.api cart.api payment.api"

# Response will contain access_token - copy it
# {
#   "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI...",
#   "expires_in": 3600,
#   "token_type": "Bearer",
#   "scope": "openid profile email roles product.api order.api cart.api payment.api"
# }

# Use token in API calls (replace YOUR_TOKEN with the actual token)
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Laptop Pro",
    "description": "High-performance laptop",
    "price": 1299.99,
    "stock": 50,
    "category": "Electronics",
    "imageUrl": "https://example.com/laptop.jpg"
  }'
```

**For Regular User:**
```bash
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=swagger-client&username=user@example.com&password=User@123&scope=openid profile email roles cart.api order.api"
```

</details>

<details>
<summary><strong>Using Tokens in API Calls</strong> (click to expand)</summary>

#### In Swagger UI

After clicking "Authorize" and entering your token (see Method 1 above), all API calls will automatically include the token. Just use the "Try it out" and "Execute" buttons as normal.

#### In curl

Add the `Authorization: Bearer {token}` header to your requests:

```bash
curl -X GET http://localhost:5004/api/cart \
  -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI..."
```

#### In PowerShell

Include the token in the headers:

```powershell
$headers = @{
    Authorization = "Bearer $token"
}

Invoke-RestMethod -Uri "http://localhost:5004/api/cart" -Headers $headers
```

</details>


<details>
<summary><strong>Endpoint Authorization Requirements</strong> (click to expand)</summary>

| Endpoint | Authentication Required | Admin Role Required |
|----------|------------------------|---------------------|
| **Product API** | | |
| GET /api/products | ❌ No | ❌ No |
| GET /api/products/{id} | ❌ No | ❌ No |
| GET /api/products/category/{category} | ❌ No | ❌ No |
| POST /api/products | ✅ Yes | ✅ Yes |
| PUT /api/products/{id} | ✅ Yes | ✅ Yes |
| DELETE /api/products/{id} | ✅ Yes | ✅ Yes |
| **Coupon API** | | |
| All endpoints | ❌ No | ❌ No |
| **Shopping Cart API** | | |
| All endpoints | ✅ Yes | ❌ No |
| **Order API** | | |
| GET /api/orders | ✅ Yes | ❌ No |
| GET /api/orders/{id} | ✅ Yes | ❌ No |
| GET /api/orders/by-number/{orderNumber} | ✅ Yes | ❌ No |
| POST /api/orders | ✅ Yes | ❌ No |
| POST /api/orders/{id}/cancel | ✅ Yes | ❌ No |
| PUT /api/orders/{id}/status | ✅ Yes | ✅ Yes |
| **Payment API** | | |
| All endpoints | ✅ Yes | ❌ No |
| **Identity API** | | |
| POST /api/account/register | ❌ No | ❌ No |
| POST /connect/token | ❌ No | ❌ No |

</details>

<details>
<summary><strong>Token Information</strong> (click to expand)</summary>

- **Token Lifetime:** 3600 seconds (1 hour)
- **Token Type:** Bearer
- **Issuer:** http://localhost:5001
- **Supported Scopes:**
  - `openid` - OpenID Connect
  - `profile` - User profile information
  - `email` - User email
  - `roles` - User roles (required for admin endpoints)
  - `product.api` - Product API access
  - `order.api` - Order API access
  - `cart.api` - Shopping Cart API access
  - `payment.api` - Payment API access
  - `coupon.api` - Coupon API access

**Note:** Tokens expire after 1 hour. If you get a 401 Unauthorized error, request a new token.

</details>

---

## API Reference

<details>
<summary><strong>Product API</strong> (click to expand)</summary>

**Base URL:** http://localhost:5002

#### Get All Products (No Auth Required)
```bash
curl http://localhost:5002/api/products
```

#### Get Product by ID
```bash
curl http://localhost:5002/api/products/{id}
```

#### Get Products by Category
```bash
curl http://localhost:5002/api/products/category/Electronics
```

#### Create Product (Requires Admin Auth)
```bash
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Laptop Pro",
    "description": "High-performance laptop",
    "price": 1299.99,
    "stock": 50,
    "category": "Electronics",
    "imageUrl": "https://example.com/laptop.jpg"
  }'
```

#### Update Product (Requires Admin Auth)
```bash
curl -X PUT http://localhost:5002/api/products/{id} \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Laptop Pro v2",
    "description": "Updated description",
    "price": 1199.99,
    "stock": 75,
    "category": "Electronics",
    "imageUrl": "https://example.com/laptop-v2.jpg",
    "isActive": true
  }'
```

#### Delete Product (Soft Delete, Requires Admin Auth)
```bash
curl -X DELETE http://localhost:5002/api/products/{id} \
  -H "Authorization: Bearer {token}"
```

</details>

<details>
<summary><strong>Coupon API</strong> (click to expand)</summary>

**Base URL:** http://localhost:5003

#### Get All Coupons (No Auth Required)
```bash
curl http://localhost:5003/api/coupons
```

#### Get Coupon by ID
```bash
curl http://localhost:5003/api/coupons/{id}
```

#### Create Coupon (No Auth Required)
```bash
curl -X POST http://localhost:5003/api/coupons \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SAVE20",
    "description": "20% off your order",
    "discountAmount": 20,
    "discountType": "Percentage",
    "minimumAmount": 50,
    "maximumDiscount": 100,
    "validFrom": "2024-01-01T00:00:00Z",
    "validUntil": "2025-12-31T23:59:59Z",
    "maxUsageCount": 100
  }'
```

**Discount Types:**
- `"Fixed"` - Fixed amount discount (e.g., $10 off)
- `"Percentage"` - Percentage discount (e.g., 20% off)

#### Create Fixed Amount Coupon
```bash
curl -X POST http://localhost:5003/api/coupons \
  -H "Content-Type: application/json" \
  -d '{
    "code": "FLAT50",
    "description": "$50 off orders over $200",
    "discountAmount": 50,
    "discountType": "Fixed",
    "minimumAmount": 200,
    "validFrom": "2024-01-01T00:00:00Z",
    "validUntil": "2025-12-31T23:59:59Z",
    "maxUsageCount": 50
  }'
```

#### Validate Coupon
```bash
curl -X POST http://localhost:5003/api/coupons/validate \
  -H "Content-Type: application/json" \
  -d '{
    "code": "SAVE20",
    "orderAmount": 100
  }'
```

#### Mark Coupon as Used
```bash
curl -X POST http://localhost:5003/api/coupons/{id}/use
```

</details>

<details>
<summary><strong>Shopping Cart API</strong> (click to expand)</summary>

**Base URL:** http://localhost:5004

**Note:** All Shopping Cart API endpoints require authentication.

#### Get Cart
```bash
curl http://localhost:5004/api/cart \
  -H "Authorization: Bearer {token}"
```

#### Add Item to Cart
```bash
curl -X POST http://localhost:5004/api/cart \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "productId": "guid-here",
    "quantity": 2
  }'
```

#### Remove Item from Cart
```bash
curl -X DELETE http://localhost:5004/api/cart/{productId} \
  -H "Authorization: Bearer {token}"
```

</details>

<details>
<summary><strong>Order API</strong> (click to expand)</summary>

**Base URL:** http://localhost:5005

**Note:** All Order API endpoints require authentication.

#### Get User Orders
```bash
curl http://localhost:5005/api/orders \
  -H "Authorization: Bearer {token}"
```

#### Get Order by ID
```bash
curl http://localhost:5005/api/orders/{id} \
  -H "Authorization: Bearer {token}"
```

#### Get Order by Number
```bash
curl http://localhost:5005/api/orders/by-number/{orderNumber} \
  -H "Authorization: Bearer {token}"
```

#### Create Order
```bash
curl -X POST http://localhost:5005/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "shippingAddress": "123 Main St, City, Country",
    "couponCode": "SAVE20"
  }'
```

#### Cancel Order
```bash
curl -X POST http://localhost:5005/api/orders/{id}/cancel \
  -H "Authorization: Bearer {token}"
```

#### Update Order Status (Admin Only)
```bash
curl -X PUT http://localhost:5005/api/orders/{id}/status \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {admin-token}" \
  -d '{
    "status": "Shipped"
  }'
```

**Order Statuses:** `Pending`, `Confirmed`, `Processing`, `Shipped`, `Delivered`, `Cancelled`

</details>

<details>
<summary><strong>Identity API</strong> (click to expand)</summary>

**Base URL:** http://localhost:5001

**See the [Authentication & Authorization](#authentication--authorization) section for complete details on:**
- Default admin credentials
- Registering users
- Getting JWT tokens
- Using tokens in API calls

#### Quick Reference

**Default Admin Account:**
- Email: `admin@example.com`
- Password: `Admin@123`

**Register User:**
```bash
curl -X POST http://localhost:5001/api/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "User@123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Get JWT Token:**
```bash
curl -X POST http://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=swagger-client&username=admin@example.com&password=Admin@123&scope=openid profile email roles product.api order.api cart.api payment.api"
```

</details>

<details>
<summary><strong>Gateway API Routes</strong> (click to expand)</summary>

The Gateway (http://localhost:5000) routes requests to backend services:

| Gateway Route | Backend Service |
|---------------|-----------------|
| `/products` | Product API |
| `/products/{id}` | Product API |
| `/coupons` | Coupon API |
| `/cart` | Shopping Cart API |
| `/orders` | Order API |

**Example via Gateway:**
```bash
curl http://localhost:5000/products
```

</details>

---

## Typical Testing Workflow

<details>
<summary><strong>Step-by-step testing guide</strong> (click to expand)</summary>

### 1. Start the Platform
```powershell
.\launch.ps1
```

### 2. Verify All Services are Healthy
Open http://localhost:55585 and wait for all services to show green checkmarks.

### 3. Get an Authentication Token
1. Open http://localhost:5001/swagger
2. Test with the default admin account:
   - Email: `admin@example.com`
   - Password: `Admin@123`

**OR use PowerShell:**
```powershell
$tokenResponse = Invoke-RestMethod -Uri "http://localhost:5001/connect/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type = "password"
        client_id = "swagger-client"
        username = "admin@example.com"
        password = "Admin@123"
        scope = "openid profile email roles product.api order.api cart.api payment.api"
    }
$token = $tokenResponse.access_token
Write-Host "Token obtained successfully!"
```

### 4. Test Product API (via Swagger)
1. Open http://localhost:5002/swagger
2. Click the **"Authorize"** button (lock icon) at the top
3. Paste your JWT token and click "Authorize"
4. Now test the endpoints:
   - **GET /api/products** (no auth required) - Click "Try it out" then "Execute"
   - **POST /api/products** (admin required) - Create a product:
     ```json
     {
       "name": "Laptop Pro",
       "description": "High-performance laptop",
       "price": 1299.99,
       "stock": 50,
       "category": "Electronics",
       "imageUrl": "https://example.com/laptop.jpg"
     }
     ```

### 5. Test Coupon API
```bash
# Create a coupon (no auth required)
curl -X POST http://localhost:5003/api/coupons \
  -H "Content-Type: application/json" \
  -d '{
    "code": "WELCOME10",
    "description": "Welcome discount",
    "discountAmount": 10,
    "discountType": "Percentage",
    "validFrom": "2024-01-01T00:00:00Z",
    "validUntil": "2025-12-31T23:59:59Z",
    "maxUsageCount": 1000
  }'

# Verify coupon was created
curl http://localhost:5003/api/coupons
```

### 6. Test Shopping Cart API (Requires Auth)
1. Open http://localhost:5004/swagger
2. Click **"Authorize"** and paste your token
3. Test endpoints:
   - **GET /api/cart** - View your cart
   - **POST /api/cart** - Add items to cart

### 7. Test Order API (Requires Auth)
1. Open http://localhost:5005/swagger
2. Click **"Authorize"** and paste your token
3. Test endpoints:
   - **GET /api/orders** - View your orders
   - **POST /api/orders** - Create a new order from your cart

### 8. Monitor with Dashboard
Keep http://localhost:55585 open to monitor service health and metrics.

</details>

---

## Infrastructure Access

<details>
<summary><strong>RabbitMQ & PostgreSQL</strong> (click to expand)</summary>

### RabbitMQ Management UI
- **URL:** http://localhost:15672
- **Username:** admin
- **Password:** admin

Use to monitor message queues, exchange bindings, and consumer connections.

### PostgreSQL Database
```bash
# Connect to database container
docker exec -it postgres psql -U postgres -W
# Password: 1111

# List databases
\l

# Connect to a specific database
\c ProductDb
\c CouponDb
\c OrderDb

# List tables
\dt

# Query data
SELECT * FROM "Products";
SELECT * FROM "Coupons";
```

</details>

---

## Troubleshooting

<details>
<summary><strong>Common issues and solutions</strong> (click to expand)</summary>

### Services Show as "Offline"
1. Check if Docker is running
2. Verify containers are up: `docker ps`
3. Check logs: `docker-compose logs -f`
4. Wait 30-60 seconds for full startup

### API Returns 401 Unauthorized
**Cause:** The endpoint requires authentication or your token is invalid/expired.

**Solutions:**
1. **Get a fresh JWT token** - See [Authentication & Authorization](#authentication--authorization) section
2. **Use the default admin account:**
   - Username: `admin@example.com`
   - Password: `Admin@123`
3. **Check token expiration** - Tokens expire after 1 hour
4. **Verify the token is included** in the `Authorization: Bearer {token}` header
5. **Use Swagger UI's Authorize button** for easier token management

**Quick token request:**
```powershell
# PowerShell
$tokenResponse = Invoke-RestMethod -Uri "http://localhost:5001/connect/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type = "password"
        client_id = "swagger-client"
        username = "admin@example.com"
        password = "Admin@123"
        scope = "openid profile email roles product.api order.api cart.api payment.api"
    }
$token = $tokenResponse.access_token
```

### API Returns 403 Forbidden
**Cause:** You are authenticated but don't have the required role (usually Admin).

**Solutions:**
1. **Use the admin account** (`admin@example.com` / `Admin@123`) for admin-only endpoints
2. **Check endpoint requirements** in the [Endpoint Authorization Requirements](#endpoint-authorization-requirements) table
3. **Verify your token includes the `roles` scope** when requesting it

### API Returns 500 Internal Server Error
1. Check service logs: `docker logs {service-name}`
2. Verify database is running: `docker logs postgres`
3. Check RabbitMQ: `docker logs rabbitmq`

### Reset Everything
```bash
docker-compose down -v    # Stop and remove volumes
docker-compose up -d      # Restart with fresh databases
```

### Port Already in Use
```powershell
# Windows PowerShell
netstat -ano | findstr :5002

# Linux/Mac
lsof -i :5002
```

### Frontend Won't Start
```bash
cd client-app
rm -rf node_modules package-lock.json
npm install
npm run dev
```

</details>

---

## Unit Tests

The project includes **95 unit tests** across 4 test projects, using **MSTest** with **Moq** for mocking and **EF Core InMemory** for database testing.

### Test Projects Overview

| Test Project | Tests | Test Files | Coverage Area |
|-------------|-------|------------|--------------|
| **Product.API.Tests** | 19 | ProductsControllerTests, ProductModelTests | CRUD operations, model defaults |
| **Order.API.Tests** | 36 | OrdersControllerTests, OrderServiceTests, OrderModelTests | Endpoints, business logic, status transitions |
| **ShoppingCart.API.Tests** | 27 | CartControllerTests, CartServiceTests, CartModelTests | Endpoints, cart operations, price calculations |
| **Gateway.Tests** | 13 | OcelotConfigurationTests | Route config, HTTP methods, downstream settings |

### Running Tests

<details>
<summary><strong>Test commands</strong> (click to expand)</summary>

**Run all tests:**
```bash
dotnet test MicroservicesECommerce.sln
```

**Run a specific test project:**
```bash
dotnet test Product.API.Tests/Product.API.Tests.csproj
dotnet test Order.API.Tests/Order.API.Tests.csproj
dotnet test ShoppingCart.API.Tests/ShoppingCart.API.Tests.csproj
dotnet test Gateway.Tests/Gateway.Tests.csproj
```

**Run with detailed output:**
```bash
dotnet test MicroservicesECommerce.sln --verbosity detailed
```

</details>

### What Is Tested

<details>
<summary><strong>Product.API.Tests (19 tests)</strong> (click to expand)</summary>

**ProductsControllerTests (13 tests):**
- `GetProducts` - Returns active products, handles empty list
- `GetProduct` - Returns product by ID, handles not found, filters inactive products
- `CreateProduct` - Returns created result, verifies database save
- `UpdateProduct` - Updates existing product, handles not found
- `DeleteProduct` - Soft-deletes product (sets IsActive=false), handles not found
- `GetByCategory` - Filters by category, handles no matches

**ProductModelTests (6 tests):**
- Default values (Id, Name, Price, Stock, Category, IsActive)
- Property setter verification
- IsActive defaults to true
- Price accepts decimal and zero values
- Stock can be negative (documents current behavior)

</details>

<details>
<summary><strong>Order.API.Tests (36 tests)</strong> (click to expand)</summary>

**OrderServiceTests (14 tests):**
- `GetByIdAsync` - Returns order for user, handles not found, prevents cross-user access
- `GetByNumberAsync` - Returns order by number, handles not found
- `GetOrdersByUserIdAsync` - Returns user orders, verifies descending date sort
- `UpdateOrderStatusAsync` - Valid transitions, handles not found, prevents invalid transitions
- `CancelOrderAsync` - Cancels pending/confirmed orders, prevents cancelling shipped orders, handles not found

**OrdersControllerTests (14 tests):**
- `GetOrders` - Returns user orders, returns 401 for unauthenticated users
- `GetOrder` - Returns order by ID, handles not found
- `CreateOrder` - Returns created result, returns 400 for empty cart or invalid coupon
- `UpdateOrderStatus` - Returns 204 on success, handles not found
- `GetOrderByNumber` - Returns order by number, handles not found
- `CancelOrder` - Returns 204 on success, returns 400 when order cannot be cancelled

**OrderModelTests (8 tests):**
- Order, OrderItem, ShippingAddress, PaymentInfo - default values and property setters
- OrderStatus enum values (Pending=0, Confirmed=1, Processing=2, Shipped=3, Delivered=4, Cancelled=5, Refunded=6)

</details>

<details>
<summary><strong>ShoppingCart.API.Tests (27 tests)</strong> (click to expand)</summary>

**CartControllerTests (10 tests):**
- `GetCart` - Returns cart with items, returns 401 for unauthenticated users
- `AddToCart` - Returns updated cart, returns 400 when product not found
- `UpdateCartItem` - Updates quantity, returns 400 when item not found
- `RemoveFromCart` - Returns updated cart after removal
- `ClearCart` - Returns 204 on success
- `GetCartItemCount` - Returns correct count, returns 0 for empty cart

**CartServiceTests (8 tests):**
- `GetCartAsync` - Returns existing cart, creates new cart for new users
- `UpdateCartItemAsync` - Updates item quantity, removes item when quantity=0, throws when item not found
- `RemoveFromCartAsync` - Removes specific item, handles non-existent item
- `ClearCartAsync` - Removes all items and resets price

**CartModelTests (9 tests):**
- Cart, CartItem, AddToCartRequest, UpdateCartRequest - default values and property setters
- TotalPrice calculation: (quantity * unitPrice) across all items

</details>

<details>
<summary><strong>Gateway.Tests (13 tests)</strong> (click to expand)</summary>

**OcelotConfigurationTests (13 tests):**
- Configuration structure: Routes array exists, GlobalConfiguration exists
- Route existence: Products, Coupons, Cart, Orders routes are configured
- HTTP methods: Products supports GET/POST, Products/{id} supports GET/PUT/DELETE, Cart supports GET/POST/DELETE
- Downstream settings: All routes use HTTP scheme, all routes use port 80
- Route mapping: All downstream paths start with /api/, all upstream paths start with /

</details>

### Test Stack

| Component | Package | Version |
|-----------|---------|---------|
| Test Framework | MSTest | 3.1.1 |
| Mocking | Moq | 4.20.70 |
| In-Memory Database | Microsoft.EntityFrameworkCore.InMemory | 8.0.0 |
| Code Coverage | coverlet.collector | 6.0.0 |

---

## Development Commands

<details>
<summary><strong>Docker commands reference</strong> (click to expand)</summary>

```bash
# Start all services
docker-compose up -d

# Rebuild specific service
docker-compose up -d --build product.api

# View logs
docker-compose logs -f product.api

# Restart service
docker-compose restart product.api

# Stop all services
docker-compose down

# Stop and remove all data
docker-compose down -v
```

</details>

---

## Architecture Diagram

```
                    +---------------------+
                    |     Dashboard       |
                    |  localhost:55585    |
                    +---------+-----------+
                              |
                    +---------v-----------+
                    |    Gateway API      |
                    |   localhost:5000    |
                    +---------+-----------+
                              |
       +----------+-----------+-----------+----------+
       |          |           |           |          |
+------v-----+ +--v---+ +-----v----+ +----v----+ +---v-----+
|  Identity  | |Product| |  Coupon  | |  Cart   | |  Order  |
|    :5001   | | :5002 | |   :5003  | |  :5004  | |  :5005  |
+------+-----+ +--+---++ +-----+----+ +---+-----+ +---+-----+
       |          |           |           |           |
       +----------+-----------+-----------+----------+
                  |                       |
        +---------v-----------+ +---------v-----------+
        |     PostgreSQL      | |      RabbitMQ       |
        |   localhost:5432    | |   localhost:15672   |
        +---------------------+ +---------------------+

```

---

## Service Endpoints Quick Reference

| Service | Base URL | Swagger | Health |
|---------|----------|---------|--------|
| Gateway | http://localhost:5000 | http://localhost:5000/swagger | http://localhost:5000/api/health |
| Identity | http://localhost:5001 | http://localhost:5001/swagger | http://localhost:5001/api/health |
| Product | http://localhost:5002 | http://localhost:5002/swagger | http://localhost:5002/api/health |
| Coupon | http://localhost:5003 | http://localhost:5003/swagger | http://localhost:5003/api/health |
| Cart | http://localhost:5004 | http://localhost:5004/swagger | http://localhost:5004/api/health |
| Order | http://localhost:5005 | http://localhost:5005/swagger | http://localhost:5005/api/health |
| Payment | http://localhost:5007 | http://localhost:5007/swagger | http://localhost:5007/api/health |
