# MoneyFex - Quick Access Guide

## 🚀 Quick Access

### Web Application
**URL**: `https://localhost:5003`

**What you can do:**
- View transaction dashboard
- Browse all transactions
- Filter and search transactions
- View transaction details
- Navigate by transaction type (Bank, Mobile, Cash, KiiBank)

## 📋 Step-by-Step Access

### Step 1: Open Swagger UI

1. Open your web browser (Chrome, Edge, Firefox, etc.)
2. Navigate to: `https://localhost:5001/swagger`
3. You may see a security warning (self-signed certificate) - click "Advanced" and "Proceed" or "Accept"
4. You should see the Swagger UI with all API endpoints

### Step 2: Test an API Endpoint

1. In Swagger UI, find the `GET /api/transactions` endpoint
2. Click on it to expand
3. Click the "Try it out" button
4. Optionally set parameters (pageNumber, pageSize, etc.)
5. Click "Execute"
6. View the response below

### Step 3: Open Web Application

1. Open a new browser tab (or window)
2. Navigate to: `https://localhost:5003`
3. You may see a security warning - click "Advanced" and "Proceed"
4. You should see the MoneyFex home page

### Step 4: Browse Transactions

1. Click "All Transactions" in the navigation menu
2. Use filters to search:
   - Search by Receipt No
   - Filter by Sender ID
   - Filter by Date Range
   - Filter by Status
3. Click "View" on any transaction to see details

## 🔍 Testing Different Transaction Types

### Bank Account Deposits
- **API**: `GET /api/bankaccountdeposits`
- **Web**: Click "Transaction Types" → "Bank Deposits"

### Mobile Money Transfers
- **API**: `GET /api/mobilemoneytransfers`
- **Web**: Click "Transaction Types" → "Mobile Transfers"

### Cash Pickups
- **API**: `GET /api/cashpickups`
- **Web**: Click "Transaction Types" → "Cash Pickups"

### KiiBank Transfers
- **API**: `GET /api/kiibanktransfers`
- **Web**: Click "Transaction Types" → "KiiBank Transfers"

## ⚠️ Troubleshooting

### If Swagger doesn't load:

1. **Check API is running**
   - Look at the terminal where you ran `dotnet run` for MoneyFex.API
   - You should see: "Now listening on: https://localhost:5001"

2. **Check for errors**
   - Look for any error messages in the console
   - Common issues: Database connection errors, port conflicts

3. **Try HTTP instead of HTTPS**
   - `http://localhost:5000/swagger`

### If Web App doesn't load:

1. **Check Web is running**
   - Look at the terminal where you ran `dotnet run` for MoneyFex.Web
   - You should see: "Now listening on: https://localhost:5003"

2. **Check for errors**
   - Look for any error messages in the console
   - Common issues: Database connection errors, port conflicts

3. **Try HTTP instead of HTTPS**
   - `http://localhost:5002`

### If you see "This site can't be reached":

1. **Verify projects are running**
   - Check both terminal windows
   - Look for "Now listening on" messages

2. **Check firewall**
   - Windows Firewall might be blocking the ports
   - Allow the applications through firewall

3. **Check port conflicts**
   - Another application might be using ports 5001/5003
   - Check console output for actual ports being used

### If you see database errors:

1. **Verify PostgreSQL is running**
   ```powershell
   psql -U postgres -c "SELECT version();"
   ```

2. **Check connection string**
   - Verify password in `appsettings.json` files
   - Ensure database `moneyfex_db` exists

3. **Check migration status**
   - Look for migration messages in console output
   - Should see "Applying migration..." or similar

## 📝 Sample API Calls

### Get All Transactions
```
GET https://localhost:5001/api/transactions
```

### Get Transaction by ID
```
GET https://localhost:5001/api/transactions/1
```

### Search Transactions
```
GET https://localhost:5001/api/transactions/search?searchTerm=ABC123
```

### Get Transactions by Sender
```
GET https://localhost:5001/api/transactions/sender/1?pageNumber=1&pageSize=10
```

### Get Bank Deposits
```
GET https://localhost:5001/api/bankaccountdeposits
```

## 🎯 Quick Test Checklist

- [ ] Swagger UI loads at `https://localhost:5001/swagger`
- [ ] Can see all API endpoints in Swagger
- [ ] Can execute a test API call in Swagger
- [ ] Web App loads at `https://localhost:5003`
- [ ] Can see home page with navigation
- [ ] Can navigate to "All Transactions"
- [ ] Can view transaction details
- [ ] No errors in browser console (F12)
- [ ] No errors in application console

## 💡 Tips

1. **Use Swagger for API Testing**
   - Swagger UI is the easiest way to test APIs
   - No need for Postman or other tools initially

2. **Check Browser Console**
   - Press F12 to open developer tools
   - Check Console tab for any JavaScript errors
   - Check Network tab to see API calls

3. **Check Application Logs**
   - Look at the terminal output
   - Errors will be displayed there
   - Migration status will be shown

4. **Start with Simple Endpoints**
   - Try `GET /api/transactions` first
   - Then try more specific endpoints
   - Test with different parameters

## 🎉 You're Ready!

Once you can access both:
- ✅ Swagger UI at `https://localhost:5001/swagger`
- ✅ Web App at `https://localhost:5003`

You're all set to use the MoneyFex system!

---

**Need Help?** Check:
- `RUNNING_STATUS.md` - Current status
- `QUICK_START.md` - Setup instructions
- `TROUBLESHOOTING.md` - Common issues

