# Email Configuration for OTP Sending

## Setup Guide

### Using Gmail SMTP

1. **Enable 2-Factor Authentication** on your Gmail account:
   - Go to https://myaccount.google.com/security
   - Enable "2-Step Verification"

2. **Create App Password**:
   - Go to https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer"
   - Google will generate a 16-character password

3. **Update appsettings.json**:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EmailFrom": "your-email@gmail.com",
    "EmailPassword": "your-16-character-app-password"
  }
}
```

### Using Other SMTP Providers

#### SendGrid
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "EmailFrom": "your-email@example.com",
    "EmailPassword": "SG.your-sendgrid-api-key"
  }
}
```

#### Office 365
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.office365.com",
    "SmtpPort": 587,
    "EmailFrom": "your-email@outlook.com",
    "EmailPassword": "your-password"
  }
}
```

## Testing

After configuration, test by:
1. Running the application: `dotnet run`
2. Navigate to login page
3. Click "Sign up with Google"
4. Enter a test email address
5. Check if OTP email is received

## Troubleshooting

- **Email not received**: Check spam/junk folder
- **Authentication failed**: Verify EmailFrom and EmailPassword are correct
- **SMTP connection error**: Check SmtpServer and SmtpPort are correct
- **Check console logs**: Look for `[EMAIL SUCCESS]` or `[EMAIL ERROR]` messages

## Important Notes

⚠️ **Security**: 
- Never commit `appsettings.json` with real passwords to git
- Use Environment Variables or User Secrets for production

For User Secrets:
```bash
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:EmailPassword" "your-app-password"
```

Then in appsettings.json, use configuration binding to load from secrets.
