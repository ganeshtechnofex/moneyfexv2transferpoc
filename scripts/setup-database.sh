#!/bin/bash
# Database Setup Script for Linux/Mac
# This script helps set up the PostgreSQL database

USERNAME=${1:-postgres}
PASSWORD=${2:-}
HOST=${3:-localhost}
PORT=${4:-5432}
DATABASE=${5:-moneyfex_db}

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"

echo "========================================"
echo "MoneyFex Database Setup"
echo "========================================"
echo ""

# Check if psql is available
if ! command -v psql &> /dev/null; then
    echo "✗ PostgreSQL (psql) not found in PATH"
    echo "  Please ensure PostgreSQL is installed and psql is in your PATH"
    exit 1
fi

echo "Database Configuration:"
echo "  Host: $HOST"
echo "  Port: $PORT"
echo "  Database: $DATABASE"
echo "  Username: $USERNAME"
echo ""

# Set PGPASSWORD if provided
if [ -n "$PASSWORD" ]; then
    export PGPASSWORD=$PASSWORD
fi

# Create database
echo "Creating database '$DATABASE'..."
if [ -n "$PASSWORD" ]; then
    psql -h $HOST -p $PORT -U $USERNAME -c "CREATE DATABASE $DATABASE;" 2>&1
else
    psql -h $HOST -p $PORT -U $USERNAME -c "CREATE DATABASE $DATABASE;" 2>&1
fi

if [ $? -eq 0 ] || psql -h $HOST -p $PORT -U $USERNAME -lqt | cut -d \| -f 1 | grep -qw $DATABASE; then
    echo "✓ Database '$DATABASE' is ready"
else
    echo "⚠ Database may already exist or creation failed"
fi

# Run schema script
echo ""
echo "Running schema script..."
SCHEMA_PATH="$PROJECT_ROOT/Database/Schema/01_CreateDatabase.sql"

if [ -f "$SCHEMA_PATH" ]; then
    if [ -n "$PASSWORD" ]; then
        psql -h $HOST -p $PORT -U $USERNAME -d $DATABASE -f "$SCHEMA_PATH"
    else
        psql -h $HOST -p $PORT -U $USERNAME -d $DATABASE -f "$SCHEMA_PATH"
    fi
    
    if [ $? -eq 0 ]; then
        echo "✓ Schema script executed successfully"
    else
        echo "✗ Schema script execution failed"
        exit 1
    fi
else
    echo "✗ Schema script not found at: $SCHEMA_PATH"
    exit 1
fi

echo ""
echo "========================================"
echo "Database Setup Complete!"
echo "========================================"
echo ""
echo "Connection String:"
echo "Host=$HOST;Port=$PORT;Database=$DATABASE;Username=$USERNAME;Password=YOUR_PASSWORD"
echo ""
echo "Update this in:"
echo "  - MoneyFex.Web/appsettings.json"
echo ""
