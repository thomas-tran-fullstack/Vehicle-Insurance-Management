using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace VehicleInsuranceAPI.Backend.LoginUserManagement
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _emailFrom;
        private readonly string _emailPassword;

        public EmailService(IConfiguration configuration)
        {
            _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
            _emailFrom = configuration["EmailSettings:EmailFrom"] ?? "";
            _emailPassword = configuration["EmailSettings:EmailPassword"] ?? "";
        }

        public async Task SendOTPAsync(string toEmail, string otp, string userName)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Timeout = 10000; // 10 seconds timeout
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailFrom, _emailPassword);

                    Console.WriteLine($"[EMAIL DEBUG] Attempting to send OTP to {toEmail}");
                    Console.WriteLine($"[EMAIL DEBUG] SMTP Server: {_smtpServer}:{_smtpPort}, From: {_emailFrom}");

                    var subject = "InsureDrive - Your OTP Verification Code";
                    var body = $@"
                        <html>
                            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                    <div style='background: linear-gradient(135deg, #137fec 0%, #0c5ac4 100%); color: white; padding: 30px; border-radius: 8px; text-align: center;'>
                                        <h1 style='margin: 0; font-size: 28px;'>InsureDrive</h1>
                                        <p style='margin: 10px 0 0 0; font-size: 14px; opacity: 0.9;'>Vehicle Insurance Management</p>
                                    </div>

                                    <div style='padding: 30px; background: white; border: 1px solid #e0e0e0;'>
                                        <h2 style='color: #333; margin-top: 0;'>Hello {userName},</h2>
                                        
                                        <p style='color: #666; margin: 15px 0;'>
                                            You've requested to verify your email address. Your one-time password (OTP) is:
                                        </p>

                                        <div style='background: #f5f5f5; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0; border-left: 4px solid #137fec;'>
                                            <p style='margin: 0; color: #666; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Your OTP Code</p>
                                            <p style='margin: 10px 0 0 0; font-size: 36px; font-weight: bold; color: #137fec; letter-spacing: 4px;'>{otp}</p>
                                        </div>

                                        <p style='color: #666; margin: 15px 0;'>
                                            <strong>⏱️ Important:</strong> This code expires in <strong>5 minutes</strong>. If you didn't request this code, please ignore this email.
                                        </p>

                                        <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 30px 0;' />

                                        <p style='color: #999; font-size: 12px; margin: 15px 0;'>
                                            If you have any questions, please contact our support team at support@insuredrive.com
                                        </p>

                                        <p style='color: #999; font-size: 12px; margin: 15px 0 0 0;'>
                                            This is an automated email. Please do not reply directly to this message.
                                        </p>
                                    </div>

                                    <div style='background: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #e0e0e0;'>
                                        <p style='color: #999; font-size: 11px; margin: 0;'>
                                            © 2026 InsureDrive. All rights reserved. | 
                                            <a href='#' style='color: #137fec; text-decoration: none;'>Privacy Policy</a> | 
                                            <a href='#' style='color: #137fec; text-decoration: none;'>Terms of Service</a>
                                        </p>
                                    </div>
                                </div>
                            </body>
                        </html>
                    ";

                    using (var message = new MailMessage(_emailFrom, toEmail))
                    {
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        await client.SendMailAsync(message);
                        Console.WriteLine($"[EMAIL SUCCESS] OTP sent successfully to {toEmail}");
                    }
                }
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst || ex.Message.Contains("Authentication"))
            {
                Console.WriteLine($"[EMAIL ERROR] Authentication failed for {_emailFrom}: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Make sure you're using an App Password, not your regular Gmail password");
                throw;
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"[EMAIL ERROR] SMTP Error sending to {toEmail}: {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Status: {ex.StatusCode}");
                throw;
            }
            catch (Exception ex)
            {
                // Log error - for now just console
                Console.WriteLine($"[EMAIL ERROR] Failed to send OTP to {toEmail}: {ex.Message}");
                throw;
            }
        }
    }
}
