#!/bin/bash
# Run Web Script for Linux/Mac
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
cd "$PROJECT_ROOT/MoneyFex.Web"
echo "Starting MoneyFex Web Application..."
echo ""
dotnet run
