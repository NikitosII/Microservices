# Microservices E-Commerce Platform

A microservices-based e-commerce platform built with ASP.NET Core.

## Architecture

This solution consists of the following microservices:

- **Gateway.API** - API Gateway using Ocelot
- **Identity.API** - Authentication and authorization service
- **Product.API** - Product management service
- **Coupon.API** - Coupon and discount management service
- **ShoppingCart.API** - Shopping cart service
- **Order.API** - Order management service
- **Email.API** - Email notification service
- **Payment.API** - Payment processing service
- **EventBus** - Event bus for inter-service communication

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
```

## API Testing

Use the `test-api.http` file for testing API endpoints with REST Client extension in VS Code or similar tools.

