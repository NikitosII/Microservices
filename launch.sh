#!/bin/bash
# Launch Script (Linux/Mac)

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

echo -e "${CYAN}========================================"
echo -e "  Microservices E-Commerce Platform    "
echo -e "========================================${NC}"
echo ""

# Check if Docker is running
echo -e "${YELLOW}[1/4] Checking Docker...${NC}"
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}✗ Docker is not running. Please start Docker first.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker is running${NC}"

# Check if docker-compose file exists
if [ ! -f "docker-compose.yml" ]; then
    echo -e "${RED}✗ docker-compose.yml not found!${NC}"
    exit 1
fi

# Start microservices with Docker Compose
echo ""
echo -e "${YELLOW}[2/4] Starting microservices...${NC}"
echo -e "${GRAY}This may take a few minutes on first run (downloading images)...${NC}"
docker-compose up -d

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Failed to start microservices${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Microservices containers started${NC}"

# Wait for services to be healthy
echo ""
echo -e "${YELLOW}[3/4] Waiting for services to be ready...${NC}"
echo -e "${GRAY}This may take 30-60 seconds...${NC}"

max_attempts=60
attempt=0
services_ready=false

while [ $attempt -lt $max_attempts ] && [ "$services_ready" = false ]; do
    sleep 2
    attempt=$((attempt + 1))

    # Check Gateway health
    if curl -s -f http://localhost:5000/health > /dev/null 2>&1; then
        services_ready=true
        echo -e "${GREEN}✓ Services are ready!${NC}"
    else
        echo -n "."
    fi
done

if [ "$services_ready" = false ]; then
    echo ""
    echo -e "${YELLOW}⚠ Services are taking longer than expected to start.${NC}"
    echo -e "${YELLOW}  Continuing anyway... Check the dashboard for status.${NC}"
fi

# Start frontend
echo ""
echo -e "${YELLOW}[4/4] Starting frontend dashboard...${NC}"

# Check if node_modules exists
if [ ! -d "client-app/node_modules" ]; then
    echo -e "${GRAY}Installing frontend dependencies...${NC}"
    cd client-app
    npm install
    cd ..
fi

echo -e "${GREEN}✓ Launching frontend...${NC}"
echo ""
echo -e "${CYAN}========================================"
echo -e "     Platform is starting!              "
echo -e "========================================${NC}"
echo ""
echo -e "Dashboard URL: ${GREEN}http://localhost:55585${NC}"
echo ""
echo -e "${YELLOW}Microservices:${NC}"
echo -e "${GRAY}  • Gateway API:        http://localhost:5000${NC}"
echo -e "${GRAY}  • Identity API:       http://localhost:5001${NC}"
echo -e "${GRAY}  • Product API:        http://localhost:5002${NC}"
echo -e "${GRAY}  • Coupon API:         http://localhost:5003${NC}"
echo -e "${GRAY}  • Shopping Cart API:  http://localhost:5004${NC}"
echo -e "${GRAY}  • Order API:          http://localhost:5005${NC}"
echo -e "${GRAY}  • Payment API:        http://localhost:5007${NC}"
echo ""
echo -e "${GRAY}RabbitMQ Management:  http://localhost:15672 (admin/admin)${NC}"
echo ""
echo -e "${YELLOW}Press Ctrl+C to stop the frontend (microservices will keep running)${NC}"
echo -e "To stop everything: ${CYAN}docker-compose down${NC}"
echo ""

cd client-app
npm run dev
