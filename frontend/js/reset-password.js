/**
 * Reset Password Form Handler
 * Manages password reset after OTP verification
 */

document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('resetPasswordForm');
    const submitBtn = document.getElementById('submitBtn');
    const spinner = document.getElementById('spinner');
    const btnText = document.getElementById('btnText');
    
    const newPasswordInput = document.getElementById('newPassword');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    const newPasswordError = document.getElementById('newPasswordError');
    const confirmPasswordError = document.getElementById('confirmPasswordError');
    
    // Password requirements elements
    const reqLength = document.getElementById('req-length');
    const reqUppercase = document.getElementById('req-uppercase');
    const reqLowercase = document.getElementById('req-lowercase');
    const reqNumber = document.getElementById('req-number');
    const strengthBar = document.getElementById('strengthBar');

    // Get email from sessionStorage
    const email = sessionStorage.getItem('forgotPasswordEmail');
    const username = sessionStorage.getItem('forgotPasswordUsername');
    
    if (!email) {
        showPopMessage('Session expired. Please try again.', 'error');
        setTimeout(() => {
            window.location.href = 'ForgotPassword.html';
        }, 2000);
    }

    // Password input event listener - real-time validation
    newPasswordInput.addEventListener('input', function() {
        validatePasswordStrength(this.value);
        newPasswordError.textContent = '';
        
        // Check if passwords match
        if (confirmPasswordInput.value && this.value !== confirmPasswordInput.value) {
            confirmPasswordError.textContent = 'Passwords do not match';
        } else {
            confirmPasswordError.textContent = '';
        }
    });

    // Confirm password input event listener
    confirmPasswordInput.addEventListener('input', function() {
        newPasswordError.textContent = '';
        if (this.value && newPasswordInput.value !== this.value) {
            confirmPasswordError.textContent = 'Passwords do not match';
        } else {
            confirmPasswordError.textContent = '';
        }
    });

    // Form submission handler
    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        // Clear previous errors
        newPasswordError.textContent = '';
        confirmPasswordError.textContent = '';
        
        const newPassword = newPasswordInput.value.trim();
        const confirmPassword = confirmPasswordInput.value.trim();
        
        // Validation
        if (!newPassword) {
            newPasswordError.textContent = 'New password is required';
            return;
        }
        
        if (!confirmPassword) {
            confirmPasswordError.textContent = 'Please confirm your password';
            return;
        }
        
        if (newPassword !== confirmPassword) {
            confirmPasswordError.textContent = 'Passwords do not match';
            return;
        }
        
        // Check password strength
        if (!isPasswordStrong(newPassword)) {
            newPasswordError.textContent = 'Password does not meet all requirements';
            return;
        }
        
        // Disable submit button and show spinner
        submitBtn.disabled = true;
        spinner.classList.add('active');
        btnText.textContent = 'Resetting...';
        
        try {
            const response = await fetch('/api/LoginUserManagement/reset-password', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    email: email,
                    username: username,
                    newPassword: newPassword
                })
            });
            
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to reset password');
            }
            
            const data = await response.json();
            
            if (data.success) {
                showPopMessage('Password reset successfully! Redirecting to login...', 'success');
                
                // Clear session data
                sessionStorage.removeItem('forgotPasswordEmail');
                sessionStorage.removeItem('forgotPasswordUsername');
                
                // Redirect to login page after 2 seconds
                setTimeout(() => {
                    window.location.href = 'Authenticate.html';
                }, 2000);
            } else {
                throw new Error(data.message || 'Failed to reset password');
            }
        } catch (error) {
            console.error('Error:', error);
            showPopMessage(error.message || 'An error occurred. Please try again.', 'error');
            submitBtn.disabled = false;
            spinner.classList.remove('active');
            btnText.textContent = 'Reset Password';
        }
    });
});

/**
 * Validate password strength in real-time
 */
function validatePasswordStrength(password) {
    const requirements = {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        lowercase: /[a-z]/.test(password),
        number: /[0-9]/.test(password)
    };
    
    // Update requirement items
    updateRequirementItem('req-length', requirements.length);
    updateRequirementItem('req-uppercase', requirements.uppercase);
    updateRequirementItem('req-lowercase', requirements.lowercase);
    updateRequirementItem('req-number', requirements.number);
    
    // Update strength bar
    updateStrengthBar(requirements);
}

/**
 * Update requirement item UI
 */
function updateRequirementItem(elementId, met) {
    const element = document.getElementById(elementId);
    if (met) {
        element.classList.add('met');
        element.querySelector('.icon').textContent = '✓';
    } else {
        element.classList.remove('met');
        element.querySelector('.icon').textContent = '✓';
    }
}

/**
 * Update strength bar color and width
 */
function updateStrengthBar(requirements) {
    const strengthBar = document.getElementById('strengthBar');
    const metRequirements = Object.values(requirements).filter(v => v).length;
    
    // Clear previous classes
    strengthBar.classList.remove('weak', 'medium', 'strong');
    
    if (metRequirements <= 2) {
        strengthBar.classList.add('weak');
    } else if (metRequirements === 3) {
        strengthBar.classList.add('medium');
    } else {
        strengthBar.classList.add('strong');
    }
}

/**
 * Check if password meets all requirements
 */
function isPasswordStrong(password) {
    return password.length >= 8 &&
           /[A-Z]/.test(password) &&
           /[a-z]/.test(password) &&
           /[0-9]/.test(password);
}

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
    const button = event.target.closest('button');
    
    if (input.type === 'password') {
        input.type = 'text';
        button.querySelector('.material-symbols-outlined').textContent = 'visibility_off';
    } else {
        input.type = 'password';
        button.querySelector('.material-symbols-outlined').textContent = 'visibility';
    }
}
