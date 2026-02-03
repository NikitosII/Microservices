#!/bin/bash
# Stop Microservices Platform (Linux/Mac)

CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m'

echo -e "${CYAN}========================================"
echo -e "  Stopping Microservices Platform      "
echo -e "========================================${NC}"
echo ""

read -p "Do you want to remove data volumes? (y/N): " choice

if [[ "$choice" == "y" || "$choice" == "Y" ]]; then
    echo -e "${YELLOW}Stopping containers and removing volumes...${NC}"
    docker-compose down -v
    echo -e "${GREEN}✓ All containers stopped and data removed${NC}"
else
    echo -e "${YELLOW}Stopping containers (keeping data)...${NC}"
    docker-compose down
    echo -e "${GREEN}✓ All containers stopped (data preserved)${NC}"
fi

echo ""
echo -e "${GREEN}Platform stopped successfully!${NC}"
echo ""
echo -e "To start again, run: ${CYAN}./launch.sh${NC}"
