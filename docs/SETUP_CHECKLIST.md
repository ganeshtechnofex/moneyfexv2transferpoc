# MoneyFex Modular - Setup Checklist

Use this checklist to ensure your system is properly configured and ready to use.

## Pre-Setup Requirements

- [ ] .NET 9 SDK installed
  - Verify: `dotnet --version` (should show 9.x.x)
- [ ] PostgreSQL installed and running
  - Verify: `psql --version`
  - Verify service is running
- [ ] Code editor installed (VS Code, Visual Studio, or Rider)
- [ ] Git installed (optional, for version control)

## Database Setup

- [ ] PostgreSQL service is running
- [ ] Created database `moneyfex_db`
  ```sql
  CREATE DATABASE moneyfex_db;
  ```
- [ ] Executed schema script
  ```bash
  psql -U postgres -d moneyfex_db -f Database/Schema/01_CreateDatabase.sql
  ```
- [ ] Verified tables were created
  ```sql
  \dt  -- List all tables in psql
  ```
- [ ] Verified enums were created
  ```sql
  SELECT typname FROM pg_type WHERE typtype = 'e';
  ```

## Project Configuration

### API Project (MoneyFex.API)

- [ ] Connection string updated in `appsettings.json`
- [ ] Connection string updated in `appsettings.Development.json`
- [ ] Verified connection string format:
  ```
  Host=localhost;Port=5432;Database=moneyfex_db;Username=postgres;Password=YOUR_PASSWORD
  ```

### Web Project (MoneyFex.Web)

- [ ] Connection string updated in `appsettings.json`
- [ ] Connection string matches API connection string
- [ ] API base URL configured (if using API calls from Web)

## Dependencies

- [ ] Restored NuGet packages
  ```bash
  dotnet restore
  ```
- [ ] No package restore errors
- [ ] All projects reference each other correctly

## Build Verification

- [ ] Solution builds without errors
  ```bash
  dotnet build
  ```
- [ ] No compilation warnings (or acceptable warnings)
- [ ] All projects compile successfully

## Runtime Verification

### API

- [ ] API starts without errors
  ```bash
  cd MoneyFex.API
  dotnet run
  ```
- [ ] API accessible at configured port
- [ ] Swagger UI loads at `/swagger`
- [ ] Can see all API endpoints in Swagger
- [ ] Database connection works (no connection errors in logs)

### Web

- [ ] Web app starts without errors
  ```bash
  cd MoneyFex.Web
  dotnet run
  ```
- [ ] Web app accessible at configured port
- [ ] Home page loads
- [ ] Navigation menu works
- [ ] No runtime errors in browser console

## Functionality Testing

### API Endpoints

- [ ] `GET /api/transactions` returns data (or empty array)
- [ ] `GET /api/transactions/{id}` works
- [ ] `GET /api/transactions/receipt/{receiptNo}` works
- [ ] `GET /api/bankaccountdeposits` works
- [ ] `GET /api/mobilemoneytransfers` works
- [ ] `GET /api/cashpickups` works
- [ ] `GET /api/kiibanktransfers` works

### Web Interface

- [ ] Home page displays
- [ ] "All Transactions" page loads
- [ ] Transaction filters work
- [ ] Search functionality works
- [ ] Transaction details page loads
- [ ] Bank Deposits page loads
- [ ] Mobile Transfers page loads
- [ ] Cash Pickups page loads
- [ ] KiiBank Transfers page loads

## Data Verification (Optional)

If you added sample data:

- [ ] Can query transactions from database
  ```sql
  SELECT COUNT(*) FROM transactions;
  ```
- [ ] Can view transactions in API
- [ ] Can view transactions in Web interface
- [ ] Transaction details display correctly

## Security Checklist

- [ ] Connection strings don't contain hardcoded passwords in source control
- [ ] `appsettings.json` is in `.gitignore` (or using environment variables)
- [ ] HTTPS is enabled for production
- [ ] CORS is configured appropriately

## Documentation

- [ ] Read `README.md`
- [ ] Reviewed `ARCHITECTURE.md`
- [ ] Checked `QUICK_START.md`
- [ ] Understood project structure

## Optional Enhancements

- [ ] Authentication configured (if needed)
- [ ] Logging configured
- [ ] Error handling customized
- [ ] UI styling customized
- [ ] Additional features implemented

## Troubleshooting

If any item fails:

1. **Database Issues**
   - Check PostgreSQL is running
   - Verify connection string
   - Check database exists
   - Re-run schema script

2. **Build Issues**
   - Run `dotnet clean`
   - Run `dotnet restore`
   - Run `dotnet build`
   - Check for missing packages

3. **Runtime Issues**
   - Check application logs
   - Verify connection strings
   - Check port availability
   - Review error messages

4. **API Issues**
   - Verify database connection
   - Check Swagger UI for errors
   - Review API logs

5. **Web Issues**
   - Check browser console for errors
   - Verify API is running (if Web calls API)
   - Check network tab in browser dev tools

## Final Verification

- [ ] All checklist items completed
- [ ] System runs without errors
- [ ] Can access both API and Web interfaces
- [ ] Basic functionality works
- [ ] Ready for development/testing

## ✅ System Ready!

Once all items are checked, your MoneyFex modular system is ready for:
- Development
- Testing
- Data migration
- Production deployment (after additional configuration)

---

**Last Updated**: 2024
**Version**: 1.0

