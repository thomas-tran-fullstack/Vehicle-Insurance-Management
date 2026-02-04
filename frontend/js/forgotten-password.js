/**
 * Forgot Password Form Handler
 * Manages the forgot password workflow: search user by email/username and send OTP
 */

document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('forgotPasswordForm');
    const submitBtn = document.getElementById('submitBtn');
    const spinner = document.getElementById('spinner');
    const btnText = document.getElementById('btnText');
    const emailOrUsernameInput = document.getElementById('emailOrUsername');
    const emailOrUsernameError = document.getElementById('emailOrUsernameError');

    // Form submission handler
    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        // Clear previous errors
        emailOrUsernameError.textContent = '';
        
        const emailOrUsername = emailOrUsernameInput.value.trim();
        
        // Validation
        if (!emailOrUsername) {
            emailOrUsernameError.textContent = 'Please enter your email or username';
            return;
        }
        
        // Disable submit button and show spinner
        submitBtn.disabled = true;
        spinner.classList.add('active');
        btnText.textContent = 'Sending...';
        
        try {
            // First, check if user exists and get their email
            const checkUserResponse = await fetch('/api/LoginUserManagement/check-user-exists', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email: emailOrUsername
                })
            });
            
            console.log('Check user response status:', checkUserResponse.status);
            
            if (!checkUserResponse.ok) {
                throw new Error(`Failed to check user: ${checkUserResponse.status}`);
            }
            
            const userData = await checkUserResponse.json();
            console.log('User data:', userData);
            
            if (!userData.success || !userData.userExists) {
                showPopMessage('User not found. Please check your email or username.', 'error');
                emailOrUsernameError.textContent = 'User not found';
                submitBtn.disabled = false;
                spinner.classList.remove('active');
                btnText.textContent = 'Send Reset Code';
                return;
            }
            
            // User exists, now send OTP
            const sendOtpResponse = await fetch('/api/LoginUserManagement/forgot-password', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email: userData.email,
                    username: emailOrUsername
                })
            });
            
            if (!sendOtpResponse.ok) {
                const errorData = await sendOtpResponse.json();
                throw new Error(errorData.message || 'Failed to send reset code');
            }
            
            const otpData = await sendOtpResponse.json();
            
            if (otpData.success) {
                showPopMessage('Reset code sent to your email. Please check your inbox.', 'success');
                
                // Store email for use in OTP verification page
                sessionStorage.setItem('forgotPasswordEmail', userData.email);
                sessionStorage.setItem('forgotPasswordUsername', emailOrUsername);
                
                // Redirect to OTP verification page after 2 seconds
                setTimeout(() => {
                    window.location.href = 'VerifyOTP.html?mode=forgot-password';
                }, 2000);
            } else {
                throw new Error(otpData.message || 'Failed to send reset code');
            }
        } catch (error) {
            console.error('Error:', error);
            showPopMessage(error.message || 'An error occurred. Please try again.', 'error');
            submitBtn.disabled = false;
            spinner.classList.remove('active');
            btnText.textContent = 'Send Reset Code';
        }
    });
    
    // Real-time validation
    emailOrUsernameInput.addEventListener('input', function() {
        if (this.value.trim()) {
            emailOrUsernameError.textContent = '';
        }
    });
});

/**
 * Show pop-up message notification
 */
function showPopMessage(message, type = 'info', duration = 5000) {
    const container = document.body;
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `pop-message ${type}`;
    messageDiv.innerHTML = `
        <span class="material-symbols-outlined">
            ${type === 'success' ? 'check_circle' : type === 'error' ? 'error' : 'info'}
        </span>
        <span>${message}</span>
    `;
    
    container.appendChild(messageDiv);
    
    // Auto-remove message after duration
    setTimeout(() => {
        messageDiv.classList.add('fade-out');
        setTimeout(() => {
            messageDiv.remove();
        }, 300);
    }, duration);
}

/**
 * Toggle password visibility
 */
function togglePasswordVisibility(inputId) {
    const input = document.getElementById(inputId);
    if (input.type === 'password') {
        input.type = 'text';
    } else {
        input.type = 'password';
    }
}
