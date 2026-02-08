// Force light mode by removing dark class (project requirement: light mode only)
if (document.documentElement.classList.contains('dark')) {
    document.documentElement.classList.remove('dark');
}

// Flag to indicate header script has loaded
window.headerScriptLoaded = false;

function initializeHeader() {
    const userMenuBtn = document.getElementById('userMenuBtn');
    const userMenuDropdown = document.getElementById('userMenuDropdown');
    let hideDropdownTimeout;

    if (userMenuBtn && userMenuDropdown) {
        // Show dropdown on hover
        userMenuBtn.addEventListener('mouseenter', function (e) {
            clearTimeout(hideDropdownTimeout);
            userMenuDropdown.classList.add('active');
        });

        // Hide dropdown with 500ms delay on mouse leave
        userMenuBtn.addEventListener('mouseleave', function (e) {
            hideDropdownTimeout = setTimeout(() => {
                userMenuDropdown.classList.remove('active');
            }, 500);
        });

        // Keep dropdown open when hovering over it
        userMenuDropdown.addEventListener('mouseenter', function (e) {
            clearTimeout(hideDropdownTimeout);
        });

        // Hide dropdown when leaving the dropdown area
        userMenuDropdown.addEventListener('mouseleave', function (e) {
            hideDropdownTimeout = setTimeout(() => {
                userMenuDropdown.classList.remove('active');
            }, 200);
        });

        // Close on click outside
        document.addEventListener('click', function (e) {
            if (!userMenuBtn.contains(e.target) && !userMenuDropdown.contains(e.target)) {
                userMenuDropdown.classList.remove('active');
            }
        });
    }

    // Check login status
    checkLoginStatus();
    
    // Load notification count
    loadNotificationCount();
}

function checkLoginStatus() {
    const user = getValidSessionUser();
    const token = localStorage.getItem('token');
    
    const authButtonsContainer = document.getElementById('authButtonsContainer');
    const userProfileContainer = document.getElementById('userProfileContainer');

    if (!authButtonsContainer || !userProfileContainer) return;

    if (user || token) {
        // User is logged in
        authButtonsContainer.style.display = 'none';
        userProfileContainer.style.display = 'flex';

        // Set user info
        const customerNameDisplay = document.getElementById('customerNameDisplay');
        const userAvatar = document.getElementById('userAvatar');

        if (user && user.roleName === 'CUSTOMER') {
            // Load customer data from database
            loadCustomerData();
            if (userAvatar) {
                // Try to load avatar, fall back to user.png
                userAvatar.src = user.avatar || '../images/user.png';
                userAvatar.onerror = function() {
                    this.src = '../images/user.png';
                };
            }
        } else if (user && user.roleName === 'ADMIN' && user.username) {
            if (customerNameDisplay) customerNameDisplay.textContent = user.username;
            if (userAvatar) {
                userAvatar.src = '../images/admin.png';
                userAvatar.onerror = function() {
                    this.src = '../images/user.png';
                };
            }
        } else if (user && user.roleName === 'STAFF' && user.fullName) {
            if (customerNameDisplay) customerNameDisplay.textContent = user.fullName;
            if (userAvatar && user.avatar) {
                userAvatar.src = user.avatar;
                userAvatar.onerror = function() {
                    this.src = '../images/user.png';
                };
            } else if (userAvatar) {
                userAvatar.src = '../images/user.png';
            }
        } else if (token && !user) {
            // Token exists but user data not loaded - show generic user
            if (userAvatar) {
                userAvatar.src = '../images/user.png';
                userAvatar.onerror = function() {
                    this.src = '../images/user.png';
                };
            }
            if (customerNameDisplay) {
                customerNameDisplay.textContent = 'User';
            }
        }
    } else {
        // User is not logged in
        authButtonsContainer.style.display = 'flex';
        userProfileContainer.style.display = 'none';
    }
}

/**
 * Return a validated user session object (or null).
 * This prevents showing the user dropdown when localStorage contains stale/invalid data.
 */
function getValidSessionUser() {
    // Prefer sessionStorage for normal login; localStorage only when "Remember me" is checked.
    const sessionStr = sessionStorage.getItem('user');
    const localStr = localStorage.getItem('user');
    const userStr = sessionStr || localStr;
    if (!userStr) return null;

    let user;
    try {
        user = JSON.parse(userStr);
    } catch {
        sessionStorage.removeItem('user');
        localStorage.removeItem('user');
        return null;
    }

    // If session came from localStorage, only trust it when rememberMe=true
    if (!sessionStr && localStr) {
        const rememberFlag = user?.rememberMe === true;
        if (!rememberFlag) {
            localStorage.removeItem('user');
            return null;
        }
    }

    // Basic shape validation
    const userId = Number(user?.userId);
    const roleId = Number(user?.roleId);
    const roleName = String(user?.roleName || '').toUpperCase();

    const validRoleName = ['CUSTOMER', 'STAFF', 'ADMIN'].includes(roleName);
    const validIds = Number.isInteger(userId) && userId > 0 && Number.isInteger(roleId) && roleId > 0;

    if (!validRoleName || !validIds) {
        // Clear bogus session so header shows "Sign In"
        sessionStorage.removeItem('user');
        localStorage.removeItem('user');
        return null;
    }

    return user;
}

async function loadCustomerData() {
    const user = getValidSessionUser();
    if (!user || !user.userId) return;

    try {
        const response = await fetch(`/api/customerinformation/${user.userId}`);
        const data = await response.json();
        
        if (data.success && data.data) {
            const customerNameDisplay = document.getElementById('customerNameDisplay');
            const userAvatar = document.getElementById('userAvatar');
            
            if (customerNameDisplay && data.data.fullName) {
                customerNameDisplay.textContent = data.data.fullName;
            }
            
            // Update avatar if available
            if (userAvatar && data.data.avatar) {
                userAvatar.src = data.data.avatar;
                userAvatar.onerror = function() {
                    this.src = '../images/user.png';
                };
            }
        }
    } catch (error) {
        console.log('Could not load customer data');
    }
}

function logout() {
    sessionStorage.removeItem('user');
    localStorage.removeItem('user');
    window.location.href = '../user/Authenticate.html';
}

// Highlight active navigation link
function highlightActiveNav() {
    const navLinks = document.querySelectorAll('.nav-link');
    const currentPath = window.location.pathname.toLowerCase();
    
    navLinks.forEach(link => {
        link.classList.remove('active');
        const href = (link.getAttribute('href') || '').toLowerCase();
        // Match by filename without extension
        const fileName = href.split('/').pop().split('.')[0];
        const pathFileName = currentPath.split('/').pop().split('.')[0];
        
        if (fileName && pathFileName && fileName === pathFileName) {
            link.classList.add('active');
        }
    });
}

// Setup search functionality
function setupSearch() {
    const searchToggle = document.getElementById('searchToggle');
    const searchInput = document.getElementById('searchInput');
    
    if (searchToggle && searchInput) {
        searchToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            if (searchInput.style.display === 'none') {
                searchInput.style.display = 'block';
                searchInput.focus();
            } else {
                searchInput.style.display = 'none';
            }
        });
        
        // Close search input on document click
        document.addEventListener('click', () => {
            if (searchInput.style.display === 'block') {
                searchInput.style.display = 'none';
            }
        });
    }
}

// Export functions to window
window.initializeHeader = initializeHeader;
window.highlightActiveNav = highlightActiveNav;
window.setupSearch = setupSearch;

// Initialize when header script loads directly (not via fetch)
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        if (typeof initializeHeader === 'function') {
            initializeHeader();
        }
        if (typeof highlightActiveNav === 'function') {
            highlightActiveNav();
        }
        if (typeof setupSearch === 'function') {
            setupSearch();
        }
    });
} else {
    // DOM is already loaded, initialize immediately
    if (typeof initializeHeader === 'function') {
        initializeHeader();
    }
    if (typeof highlightActiveNav === 'function') {
        highlightActiveNav();
    }
    if (typeof setupSearch === 'function') {
        setupSearch();
    }
}

/**
 * Update avatar in header when user changes their profile picture
 */
function updateHeaderAvatar(newAvatarUrl) {
    const userAvatar = document.getElementById('userAvatar');
    if (userAvatar) {
        userAvatar.src = newAvatarUrl;
        userAvatar.onerror = function() {
            this.src = '../images/user.png';
        };
    }
}

// Load notification count
async function loadNotificationCount() {
    try {
        const user = getValidSessionUser();
        if (!user || !user.userId) return;

        // Set API_BASE if not already set
        if (typeof API_BASE === 'undefined' && typeof window.API_BASE === 'undefined') {
            window.API_BASE = "/api";
        }
        const apiBase = typeof API_BASE !== 'undefined' ? API_BASE : window.API_BASE;

        const response = await fetch(`${apiBase}/admin-notification/unread-count/${user.userId}`);
        if (!response.ok) return;

        const data = await response.json();
        const unreadCount = data.unreadCount || 0;

        const badge = document.getElementById('notificationBadge');
        if (badge) {
            if (unreadCount > 0) {
                badge.textContent = unreadCount > 9 ? '9+' : unreadCount;
                badge.classList.remove('hidden');
            } else {
                badge.classList.add('hidden');
            }
        }
    } catch (error) {
        console.log('Cannot load notification count:', error);
    }
}


