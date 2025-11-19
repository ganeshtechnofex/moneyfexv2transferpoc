# MoneyFex Modular - Quick Start Guide

## 🚀 Get Started in 5 Minutes

### Prerequisites Check

Before starting, ensure you have:
- ✅ .NET 9 SDK installed
- ✅ PostgreSQL 12+ installed and running
- ✅ Visual Studio 2022 / VS Code / Rider (optional)

### Step 1: Setup PostgreSQL Database

1. **Create Database**
   ```bash
   # Open PostgreSQL command line
   psql -U postgres
   
   # Create database
   CREATE DATABASE moneyfex_db;
   
   # Exit psql
   \q
   ```

2. **Run Schema Script**
   ```bash
   # From project root
   psql -U postgres -d moneyfex_db -f Database/Schema/01_CreateDatabase.sql
   ```

   Or on Windows PowerShell:
   ```powershell
   psql -U postgres -d moneyfex_db -f Database\Schema\01_CreateDatabase.sql
   ```

### Step 2: Configure Connection Strings

1. **Update Web Connection String**
   - Open `MoneyFex.Web/appsettings.json`
   - Update the connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=YOUR_PASSWORD"
   }
   ```

### Step 3: Restore Dependencies

```bash
# From project root
dotnet restore
```

### Step 4: Build the Solution

```bash
# From project root
dotnet build
```

### Step 5: Run the Web Application

```bash
cd MoneyFex.Web
dotnet run
```

The Web app will start on:
- HTTP: `http://localhost:5002`
- HTTPS: `https://localhost:5003`

### Step 6: Access the Application

**Web Application**
   - Navigate to: `https://localhost:5003`
   - Use the navigation menu to explore:
     - All Transactions
     - Bank Deposits
     - Mobile Transfers
     - Cash Pickups
     - KiiBank Transfers

## 🧪 Testing the System

### Test API Endpoints

1. **Get All Transactions**
   ```
   GET https://localhost:5001/api/transactions
   ```

2. **Get Transaction by Receipt No**
   ```
   GET https://localhost:5001/api/transactions/receipt/{receiptNo}
   ```

3. **Search Transactions**
   ```
   GET https://localhost:5001/api/transactions/search?searchTerm=ABC123
   ```

### Test Web Interface

1. **View Dashboard**
   - Navigate to home page
   - See recent transactions (if any exist)

2. **Filter Transactions**
   - Go to "All Transactions"
   - Use filters: Sender ID, Date Range, Status, Search Term

3. **View Transaction Details**
   - Click "View" on any transaction
   - See complete transaction information

## 📝 Adding Sample Data (Optional)

If you want to test with sample data, you can insert test records:

```sql
-- Insert a test country
INSERT INTO countries (country_code, country_name, currency, currency_symbol)
VALUES ('US', 'United States', 'USD', '$');

INSERT INTO countries (country_code, country_name, currency, currency_symbol)
VALUES ('GB', 'United Kingdom', 'GBP', '£');

-- Insert a test sender
INSERT INTO senders (first_name, last_name, email, phone_number, account_no, country_code, is_active)
VALUES ('John', 'Doe', 'john.doe@example.com', '+1234567890', 'MF001', 'US', true);

-- Insert sender login
INSERT INTO sender_logins (sender_id, is_active)
VALUES (1, true);

-- Insert a test bank
INSERT INTO banks (name, code, country_code, is_active)
VALUES ('Test Bank', 'TB001', 'US', true);

-- Insert a test transaction
INSERT INTO transactions (
    transaction_date, receipt_no, sender_id,
    sending_country_code, receiving_country_code,
    sending_currency, receiving_currency,
    sending_amount, receiving_amount, fee, total_amount,
    exchange_rate, sender_payment_mode, transaction_module, status
)
VALUES (
    CURRENT_TIMESTAMP, 'BD001', 1,
    'US', 'GB',
    'USD', 'GBP',
    100.00, 75.00, 5.00, 105.00,
    0.75, 'Card', 'Sender', 'InProgress'
);

-- Insert bank deposit details
INSERT INTO bank_account_deposits (
    transaction_id, bank_id, receiver_account_no, receiver_name
)
VALUES (1, 1, 'ACC123456', 'Jane Smith');
```

## 🔧 Troubleshooting

### Database Connection Issues

**Error**: "Connection string not found"
- **Solution**: Check `appsettings.json` files have correct connection strings

**Error**: "Failed to connect to database"
- **Solution**: 
  - Verify PostgreSQL is running
  - Check username/password
  - Verify database exists

### Port Already in Use

**Error**: "Address already in use"
- **Solution**: 
  - Change ports in `launchSettings.json`
  - Or stop the application using the port

### Missing Dependencies

**Error**: "Package not found"
- **Solution**: 
  ```bash
  dotnet restore
  dotnet build
  ```

### Entity Framework Issues

**Error**: "Table does not exist"
- **Solution**: 
  - Verify database schema was created
  - Check connection string points to correct database
  - Re-run schema script

## 📚 Next Steps

1. **Explore the API**
   - Use Swagger UI to test endpoints
   - Review API documentation

2. **Explore the Web Interface**
   - Navigate through different transaction types
   - Test filtering and search

3. **Review Documentation**
   - Read `README.md` for detailed information
   - Check `ARCHITECTURE.md` for system design
   - Review `MIGRATION_GUIDE.md` for data migration

4. **Customize**
   - Add authentication
   - Implement additional features
   - Customize UI styling

## 🎯 Common Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run API
cd MoneyFex.API && dotnet run

# Run Web
cd MoneyFex.Web && dotnet run

# Run both (requires multiple terminals)
# Terminal 1:
cd MoneyFex.API && dotnet run

# Terminal 2:
cd MoneyFex.Web && dotnet run

# Clean build
dotnet clean
dotnet build

# Watch mode (auto-reload on changes)
dotnet watch run
```

## ✅ Verification Checklist

- [ ] PostgreSQL is running
- [ ] Database `moneyfex_db` is created
- [ ] Schema script executed successfully
- [ ] Connection strings updated in both appsettings.json files
- [ ] Dependencies restored (`dotnet restore`)
- [ ] Solution builds successfully (`dotnet build`)
- [ ] API runs without errors
- [ ] Web app runs without errors
- [ ] Swagger UI is accessible
- [ ] Web interface is accessible

## 🎉 You're Ready!

Once all checklist items are complete, your MoneyFex modular system is ready to use!

For questions or issues, refer to:
- `README.md` - Main documentation
- `ARCHITECTURE.md` - System architecture
- `MIGRATION_GUIDE.md` - Database migration
- `COMPLETION_STATUS.md` - Feature checklist

---

**Happy Coding! 🚀**

