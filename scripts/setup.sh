#!/bin/bash
# MoneyFex Modular - Setup Script for Linux/Mac
# This script helps automate the setup process

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
cd "$PROJECT_ROOT"

echo "========================================"
echo "MoneyFex Modular - Setup Script"
echo "========================================"
echo ""

# Check if .NET SDK is installed
echo "Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✓ .NET SDK found: $DOTNET_VERSION"
else
    echo "✗ .NET SDK not found. Please install .NET 9 SDK."
    exit 1
fi

# Check if PostgreSQL is available
echo ""
echo "Checking PostgreSQL..."
if command -v psql &> /dev/null; then
    PSQL_VERSION=$(psql --version)
    echo "✓ PostgreSQL found: $PSQL_VERSION"
else
    echo "⚠ PostgreSQL not found in PATH. Please ensure PostgreSQL is installed."
    echo "  You can still continue, but you'll need to run the SQL script manually."
fi

# Restore NuGet packages
echo ""
echo "Restoring NuGet packages..."
dotnet restore
if [ $? -eq 0 ]; then
    echo "✓ Packages restored successfully"
else
    echo "✗ Failed to restore packages"
    exit 1
fi

# Build the solution
echo ""
echo "Building solution..."
dotnet build
if [ $? -eq 0 ]; then
    echo "✓ Solution built successfully"
else
    echo "✗ Build failed"
    exit 1
fi

# Database setup instructions
echo ""
echo "========================================"
echo "Database Setup Required"
echo "========================================"
echo ""
echo "Please run the following commands to set up the database:"
echo ""
echo "1. Create database:"
echo "   psql -U postgres -c 'CREATE DATABASE moneyfex_db;'"
echo ""
echo "2. Run schema script:"
echo "   psql -U postgres -d moneyfex_db -f Database/Schema/01_CreateDatabase.sql"
echo ""
echo "   Or use the database setup script:"
echo "   ./scripts/setup-database.sh"
echo ""

# Connection string reminder
echo "========================================"
echo "Connection String Configuration"
echo "========================================"
echo ""
echo "Please update connection string in:"
echo "  - MoneyFex.Web/appsettings.json"
echo ""
echo "Format:"
echo '  "ConnectionStrings": {'
echo '    "DefaultConnection": "Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=YOUR_PASSWORD"'
echo '  }'
echo ""

# Ready to run
echo "========================================"
echo "Ready to Run!"
echo "========================================"
echo ""
echo "To run the Web application:"
echo "  ./scripts/run-web.sh"
echo "  Or: cd MoneyFex.Web && dotnet run"
echo ""
echo "Web application will be available at: https://localhost:5003"
echo ""
