/**
 * PopMessage Utility - Replaces alert() with styled notification messages
 * Usage: showPopMessage('Success message', 'success')
 * Types: 'success', 'error', 'info'
 */

(function() {
    // Initialize styles once
    if (!document.getElementById('popMessageStyles')) {
        const style = document.createElement('style');
        style.id = 'popMessageStyles';
        style.innerHTML = `
            .pop-message {
                position: fixed;
                top: 100px;
                right: 20px;
                max-width: 400px;
                padding: 16px 20px;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                display: flex;
                align-items: center;
                gap: 12px;
                font-size: 14px;
                font-weight: 500;
                z-index: 1000;
                animation: slideIn 0.3s ease-out;
                font-family: 'Inter', sans-serif;
            }
            .pop-message.success {
                background-color: #dcfce7;
                color: #166534;
                border-left: 4px solid #22c55e;
            }
            .pop-message.error {
                background-color: #fee2e2;
                color: #991b1b;
                border-left: 4px solid #ef4444;
            }
            .pop-message.info {
                background-color: #dbeafe;
                color: #0c2340;
                border-left: 4px solid #3b82f6;
            }
            .pop-message .icon {
                font-size: 20px;
                flex-shrink: 0;
            }
            @keyframes slideIn {
                from {
                    transform: translateX(400px);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }
            @keyframes slideOut {
                from {
                    transform: translateX(0);
                    opacity: 1;
                }
                to {
                    transform: translateX(400px);
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
    }

    // Global function to show pop message
    window.showPopMessage = function(message, type = 'success') {
        const messageEl = document.createElement('div');
        messageEl.className = `pop-message ${type}`;
        
        let icon = '✓';
        if (type === 'error') icon = '✕';
        else if (type === 'info') icon = 'ℹ';
        
        messageEl.innerHTML = `
            <span class="icon">${icon}</span>
            <span>${message}</span>
        `;
        document.body.appendChild(messageEl);

        setTimeout(() => {
            messageEl.style.animation = 'slideOut 0.3s ease-out forwards';
            setTimeout(() => messageEl.remove(), 300);
        }, 3000);
    };
})();
