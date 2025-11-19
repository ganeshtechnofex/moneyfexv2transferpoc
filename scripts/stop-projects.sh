#!/bin/bash
# Stop Projects Script for Linux/Mac
echo "Stopping MoneyFex projects..."
echo ""

# Find and stop dotnet processes related to MoneyFex
echo "Finding MoneyFex processes..."
pkill -f "MoneyFex.Web" 2>/dev/null && echo "✓ MoneyFex.Web stopped" || echo "No MoneyFex.Web processes found"

# Wait a moment
sleep 2

echo ""
echo "========================================"
echo "Processes Stopped"
echo "========================================"
echo ""
echo "You can now:"
echo "  1. Rebuild the solution: dotnet build"
echo "  2. Restart the Web project:"
echo "     ./scripts/run-web.sh"
echo "     Or: cd MoneyFex.Web && dotnet run"
echo ""

