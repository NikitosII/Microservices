#!/bin/bash

# Bash script to launch all microservices
echo "Starting Microservices E-Commerce Platform..."

services=(
    "src/Identity/Identity.API"
    "src/Microservices/Product.API"
    "src/Microservices/Coupon.API"
    "src/Microservices/ShoppingCart.API"
    "src/Microservices/Order.API"
    "src/Microservices/Payment.API"
    "src/Gateway/Gateway.API"
)

for service in "${services[@]}"; do
    echo "Starting $service..."
    gnome-terminal -- bash -c "cd $service && dotnet run; exec bash" 2>/dev/null || \
    xterm -e "cd $service && dotnet run" 2>/dev/null || \
    osascript -e "tell app \"Terminal\" to do script \"cd $service && dotnet run\"" 2>/dev/null || \
    echo "Please start $service manually: cd $service && dotnet run"
    sleep 2
done

echo "All services are starting. Check individual terminals for status."
