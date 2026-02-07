# Setup Instructions - Vehicle Insurance Management System

## Prerequisites
- **.NET 9.0 SDK** - Download from https://dotnet.microsoft.com/download/dotnet/9.0
- **SQL Server** - SQL Server 2019+ or SQL Server Express (Local DB works too)
- **Node.js** (only if you want to modify frontend) - Optional

## Quick Start

### 1. Database Setup
The application will **automatically create the database** on first run. You don't need to run migrations manually.

**Requirements:**
- SQL Server must be running
- Default connection is to local server instance: `Server=.;Database=AVIMS_DB`
- Uses Windows Authentication (Trusted_Connection=True)

**For SQL Server Express:**
- Connection string will work automatically if SQL Server Express is installed locally
- If using a named instance like `SQLEXPRESS`, update `appsettings.json`:
  ```json
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=AVIMS_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  ```

### 2. Configure Connection String (If Needed)
Edit `appsettings.json` or `appsettings.Development.json`:

**For Local Server:**
```json
"DefaultConnection": "Server=.;Database=AVIMS_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

**For SQL Server Express:**
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=AVIMS_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

**For Remote Server (with username/password):**
```json
"DefaultConnection": "Server=YOUR_SERVER_IP;Database=AVIMS_DB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

### 3. Default Login Credentials
After the app creates the database, you can login with:
- **Username:** `admin`
- **Password:** `admin123`

The system will automatically create this admin account on first run.

### 4. Running the Application
Navigate to the project directory and run:
```bash
dotnet run
```

The application will:
1. ✅ Automatically create the database if it doesn't exist
2. ✅ Seed initial roles (ADMIN, STAFF, CUSTOMER)
3. ✅ Create default admin user
4. ✅ Start the API server on `http://localhost:5169`

### 5. Access the Application
- **Admin Panel:** http://localhost:5169/admin/dashboard.html
- **User Portal:** http://localhost:5169/
- **API Documentation:** http://localhost:5169/swagger/index.html (Development mode only)

## Troubleshooting

### Issue: "Connection timeout" or "Cannot open database"
**Solution:** 
- Check if SQL Server is running
- If using SQL Server Express, verify the service is started: `services.msc`
- Update the connection string in `appsettings.json` with correct server name/IP

### Issue: "Login failed for user 'NT AUTHORITY\NETWORK SERVICE'"
**Solution:**
- Make sure Windows Authentication is enabled for your SQL Server
- Or switch to username/password authentication in connection string

### Issue: "Port 5169 already in use"
**Solution:**
- Change the port in `Properties/launchSettings.json`
- Or kill the existing process using that port

### Issue: API returns 404 for admin-notification endpoints
**Solution:**
- Make sure the app has fully started (check console output)
- Verify you're using the correct endpoint: `/api/admin-notification/users`
- Clear browser cache and reload

## Email Configuration (Optional)
Email settings are in `appsettings.json`:
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "EmailFrom": "your-email@gmail.com",
  "EmailPassword": "your-app-password"
}
```

For Gmail, use an [App Password](https://support.google.com/accounts/answer/185833), not your regular password.

## Project Structure
```
/backend/        - .NET controllers and business logic
/frontend/       - HTML, CSS, JavaScript
/Data/           - Database context
/Models/         - Database models
/Migrations/     - Database schema (auto-generated)
appsettings.json - Configuration file
```

## Common Ports
- API Server: **5169**
- HTTPS: **7120***

## Support
If you encounter any issues:
1. Check the console output for error messages
2. Review the logs in the console window
3. Ensure all prerequisites are installed
4. Verify database connection settings in `appsettings.json`

---
Last Updated: Feb 7, 2026
