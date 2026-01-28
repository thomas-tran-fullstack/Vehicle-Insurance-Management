/**
 * Global Page Loader Script
 * Handles loading animation and 0.5-second delay for all page transitions
 * Include this script at the end of body tag in all HTML files
 */

(function() {
    // Add loading bar and spinner HTML if not already present
    function initializePageLoader() {
        // Check if loader HTML is already added
        if (document.getElementById('loadingBar')) return;
        
        // Create loading bar
        const loadingBar = document.createElement('div');
        loadingBar.id = 'loadingBar';
        loadingBar.className = 'loading-bar';
        document.body.appendChild(loadingBar);
        
        // Create page loader spinner
        const pageLoader = document.createElement('div');
        pageLoader.id = 'pageLoader';
        pageLoader.className = 'page-loader';
        pageLoader.innerHTML = `
            <div class="flex flex-col items-center justify-center">
                <div class="spinner-dots">
                    <div class="dot dot-1"></div>
                    <div class="dot dot-2"></div>
                    <div class="dot dot-3"></div>
                </div>
                <p class="spinner-text">Loading...</p>
            </div>
        `;
        document.body.appendChild(pageLoader);
        
        // Add CSS styles
        addStyles();
        
        // Setup event listeners
        setupEventListeners();
    }
    
    function addStyles() {
        // Check if styles are already added
        if (document.getElementById('pageLoaderStyles')) return;
        
        const style = document.createElement('style');
        style.id = 'pageLoaderStyles';
        style.textContent = `
            /* Loading Bar Styles */
            .loading-bar {
                position: fixed;
                top: 0;
                left: 0;
                height: 3px;
                background: linear-gradient(to right, #137fec, #0fa0ce, #137fec);
                width: 0;
                z-index: 9999;
                transition: width 0.3s ease;
                box-shadow: 0 0 10px rgba(19, 127, 236, 0.8);
            }

            .loading-bar.active {
                animation: loadingProgress 2s ease-in-out infinite;
            }

            @keyframes loadingProgress {
                0% { width: 0; }
                30% { width: 30%; }
                60% { width: 60%; }
                100% { width: 90%; }
            }

            .loading-bar.complete {
                width: 100%;
                animation: none;
                opacity: 1;
                transition: opacity 0.5s ease 0.5s;
            }

            .loading-bar.fade-out {
                opacity: 0;
                transition: opacity 0.5s ease;
            }

            /* Loading Spinner Styles */
            .page-loader {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(255, 255, 255, 0.95);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 9998;
                opacity: 0;
                transition: opacity 0.3s ease;
                pointer-events: none;
            }

            .page-loader.show {
                opacity: 1;
                pointer-events: auto;
            }

            html.dark .page-loader {
                background: rgba(16, 25, 34, 0.95);
            }

            .spinner-dots {
                display: flex;
                gap: 8px;
                align-items: flex-end;
                height: 40px;
                margin-bottom: 20px;
            }

            .dot {
                width: 8px;
                height: 8px;
                background: linear-gradient(135deg, #137fec, #0fa0ce);
                border-radius: 50%;
                animation: dotAnimation 1.4s ease-in-out infinite;
            }

            .dot-1 {
                animation-delay: 0s;
                height: 12px;
            }

            .dot-2 {
                animation-delay: 0.2s;
                height: 16px;
            }

            .dot-3 {
                animation-delay: 0.4s;
                height: 12px;
            }

            @keyframes dotAnimation {
                0%, 100% { 
                    transform: scaleY(0.8);
                    opacity: 0.6;
                }
                50% { 
                    transform: scaleY(1.2);
                    opacity: 1;
                }
            }

            .spinner-text {
                position: absolute;
                margin-top: 80px;
                color: #137fec;
                font-size: 14px;
                font-weight: 600;
                letter-spacing: 0.05em;
            }

            /* Flex utilities if not already defined */
            .flex {
                display: flex;
            }

            .flex-col {
                flex-direction: column;
            }

            .items-center {
                align-items: center;
            }

            .justify-center {
                justify-content: center;
            }
        `;
        document.head.appendChild(style);
    }
    
    let loadingTimeout;
    let loadingBarTimeout;
    
    function startLoading() {
        const loadingBar = document.getElementById('loadingBar');
        const pageLoader = document.getElementById('pageLoader');
        
        if (loadingBar) {
            loadingBar.classList.remove('complete', 'fade-out');
            loadingBar.classList.add('active');
        }
        
        if (pageLoader) {
            pageLoader.classList.add('show');
        }
        
        // Clear any existing timeouts
        clearTimeout(loadingTimeout);
        clearTimeout(loadingBarTimeout);
    }
    
    function finishLoading() {
        const loadingBar = document.getElementById('loadingBar');
        const pageLoader = document.getElementById('pageLoader');
        
        // Complete the loading bar
        if (loadingBar) {
            loadingBar.classList.remove('active');
            loadingBar.classList.add('complete');
            
            // Fade out after completion
            loadingBarTimeout = setTimeout(() => {
                loadingBar.classList.add('fade-out');
                setTimeout(() => {
                    loadingBar.classList.remove('complete', 'fade-out');
                }, 500);
            }, 500);
        }
        
        // Hide page loader
        if (pageLoader) {
            loadingTimeout = setTimeout(() => {
                pageLoader.classList.remove('show');
            }, 300);
        }
    }
    
    function setupEventListeners() {
        // Show loading on page load
        window.addEventListener('load', finishLoading);
        
        // Handle link navigation with 0.5 second delay
        document.addEventListener('click', (e) => {
            const link = e.target.closest('a');
            if (link && link.href && !link.href.includes('#') && !link.target) {
                // Check if it's an internal link (same hostname)
                try {
                    const linkUrl = new URL(link.href);
                    const currentUrl = new URL(window.location.href);
                    
                    if (linkUrl.hostname === currentUrl.hostname) {
                        e.preventDefault();
                        startLoading();
                        
                        // Delay navigation by 0.5 seconds for animation effect
                        setTimeout(() => {
                            window.location.href = link.href;
                        }, 500);
                    }
                } catch (err) {
                    console.log('Could not parse URL');
                }
            }
        });
        
        // Show loading on form submission if form action points to internal URL
        document.addEventListener('submit', (e) => {
            const form = e.target;
            const action = form.getAttribute('action');
            
            if (action && !action.includes('api') && !action.includes('http')) {
                startLoading();
            }
        });
    }
    
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializePageLoader);
    } else {
        initializePageLoader();
    }
})();
