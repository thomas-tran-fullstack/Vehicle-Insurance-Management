# 📧 Email Configuration Guide - InsureDrive OTP

## ⚠️ Current Issue
Gmail SMTP requires authentication using **App Passwords**, not your regular Gmail password.

## 🔧 Solution: Setup Gmail App Password

### Step 1: Enable 2-Factor Authentication (if not already enabled)
1. Go to https://myaccount.google.com/
2. Click **Security** on the left menu
3. Look for "2-Step Verification" and click it
4. Follow the steps to enable it (you'll need to verify with your phone)

### Step 2: Generate App Password
1. Go back to https://myaccount.google.com/
2. Click **Security** on the left menu
3. Scroll down and find **App passwords** (only appears if 2FA is enabled)
4. Click **App passwords**
5. Select: **Mail** and **Windows Computer**
6. Google will generate a 16-character password like: `xxxx xxxx xxxx xxxx`
7. **Copy this password** (including spaces or without, depending on your preference)

### Step 3: Update appsettings.json
Open `appsettings.json` and update these values:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EmailFrom": "your-email@gmail.com",
    "EmailPassword": "xxxx xxxx xxxx xxxx"
  }
}
```

**Replace:**
- `your-email@gmail.com` → Your actual Gmail address
- `xxxx xxxx xxxx xxxx` → The 16-character App Password from Step 2

### Step 4: Save and Restart
1. Save `appsettings.json`
2. Stop the dotnet server (Ctrl+C)
3. Run `dotnet run` again
4. Test Google Sign-In with OTP verification

---

## ✅ Expected Behavior

When a user clicks "Login with Google":

1. **If email exists in database** → Login directly
2. **If email is new:**
   - Backend generates 4-digit OTP
   - **Email with OTP is sent** to user's Gmail inbox
   - User is redirected to OTP verification page
   - User enters 4 digits and account is created

---

## 🐛 Troubleshooting

### Error: "Authentication Required"
- ❌ Using regular Gmail password
- ✅ Must use **App Password** (16 characters from Step 2)

### Error: "Less Secure Apps"
- Gmail has deprecated "Less Secure App Access"
- ✅ Use **App Passwords** instead (recommended)

### OTP Not Sent
1. Check console logs for `[EMAIL ERROR]` messages
2. Verify `appsettings.json` has correct credentials
3. Make sure 2FA is enabled on your Gmail
4. Make sure App Password is generated correctly

### Test Email Service
Run this endpoint to test email:
```
POST http://localhost:5169/api/test-email
Body: {"email": "your-test-email@gmail.com"}
```

---

## 📝 Other Email Providers (Alternative)

If you want to use a different email provider instead of Gmail:

### SendGrid
1. Create account: https://sendgrid.com/
2. Get API key from Dashboard
3. Update `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "EmailFrom": "noreply@insuredrive.com",
    "EmailPassword": "SG.your-sendgrid-api-key"
  }
}
```

### Office 365 / Outlook
1. Get your email: your-email@company.com
2. Update `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.office365.com",
    "SmtpPort": 587,
    "EmailFrom": "your-email@company.com",
    "EmailPassword": "your-office-password"
  }
}
```

---

## 🎯 Security Notes

1. **Never commit appsettings.json with real credentials** to GitHub
2. Use **Environment Variables** in production:
   ```bash
   set EmailSettings:EmailPassword=xxxx xxxx xxxx xxxx
   ```
3. Store passwords in **Azure Key Vault** or similar secure storage

---

## ❓ Still Having Issues?

1. Check console output for `[EMAIL DEBUG]` and `[EMAIL ERROR]` messages
2. Verify SMTP server and port are correct
3. Make sure firewall isn't blocking port 587
4. Test with a simple test endpoint first
5. Check Gmail's "Less secure app" notifications and click "Allow"

---

**Last Updated:** February 3, 2026
