#!/bin/bash
# Data Migration Script for Linux/Mac
# Migrates data from legacy SQL Server to new PostgreSQL database

echo "========================================"
echo "MoneyFex Data Migration Tool"
echo "========================================"
echo ""

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed or not in PATH"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✓ .NET SDK found: $DOTNET_VERSION"
echo ""

# Navigate to migration tool directory
SCRIPT_PATH=$(dirname "$(readlink -f "$0")")
PROJECT_ROOT=$(dirname "$SCRIPT_PATH")
MIGRATION_TOOL_PATH="$PROJECT_ROOT/MoneyFex.Infrastructure/MigrationTool"

if [ ! -d "$MIGRATION_TOOL_PATH" ]; then
    echo "ERROR: Migration tool directory not found: $MIGRATION_TOOL_PATH"
    exit 1
fi

cd "$MIGRATION_TOOL_PATH"

# Check if appsettings.json exists
if [ ! -f "appsettings.json" ]; then
    echo "ERROR: appsettings.json not found in migration tool directory"
    echo "Please create appsettings.json with connection strings"
    exit 1
fi

# Parse command line arguments
MODE="full"
if [ $# -gt 0 ]; then
    MODE=$1
fi

echo "Migration Mode: $MODE"
echo ""

# Create logs directory
LOGS_DIR="$PROJECT_ROOT/logs"
mkdir -p "$LOGS_DIR"
echo "✓ Created logs directory"
echo ""

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "ERROR: Failed to restore packages"
    exit 1
fi
echo "✓ Packages restored"
echo ""

# Build project
echo "Building migration tool..."
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi
echo "✓ Build successful"
echo ""

# Run migration
echo "Starting migration..."
echo ""

case $MODE in
    "full")
        dotnet run --configuration Release -- --mode full
        ;;
    "validate")
        dotnet run --configuration Release -- --mode validate
        ;;
    "incremental")
        BATCH_SIZE=${2:-1000}
        dotnet run --configuration Release -- --mode incremental --batch-size $BATCH_SIZE
        ;;
    *)
        echo "Usage: ./run-migration.sh [full|validate|incremental] [batch-size]"
        echo ""
        echo "Modes:"
        echo "  full        - Migrate all data (default)"
        echo "  validate    - Validate data without migrating"
        echo "  incremental - Migrate data in batches"
        exit 1
        ;;
esac

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "Migration Completed Successfully"
    echo "========================================"
    echo ""
    echo "Check logs/migration.log for details"
else
    echo ""
    echo "========================================"
    echo "Migration Failed"
    echo "========================================"
    echo ""
    echo "Check logs/migration.log for error details"
    exit 1
fi

