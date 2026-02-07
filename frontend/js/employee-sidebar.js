let currentPageName = '';

function setCurrentPage(pageName) {
    currentPageName = pageName;
    highlightActivePage();
}

function highlightActivePage() {
    const navLinks = document.querySelectorAll('aside nav a');
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        
        // Remove all active classes
        link.classList.remove('bg-primary/10', 'text-primary');
        link.classList.remove('text-slate-600', 'dark:text-slate-400');
        link.classList.add('text-slate-600', 'dark:text-slate-400');
        
        // Remove the vertical line indicator if exists
        const existingLine = link.querySelector('.ml-auto.w-1');
        if (existingLine && existingLine.classList.contains('bg-primary')) {
            existingLine.remove();
        }
        
        // Check if this link matches the current page
        // Compare both exact match and filename match
        if (href === currentPageName || currentPageName.includes(href)) {
            link.classList.remove('text-slate-600', 'dark:text-slate-400');
            link.classList.add('bg-primary/10', 'text-primary');
            
            // Update the icon color for the active link
            const icon = link.querySelector('.material-symbols-outlined');
            if (icon) {
                icon.classList.remove('text-slate-400');
                icon.classList.add('text-primary');
            }
            
            // Add the vertical line indicator
            const lineEl = document.createElement('div');
            lineEl.className = 'ml-auto w-1 h-5 bg-primary rounded-full';
            link.appendChild(lineEl);
        }
    });
}

function initializeStaffSidebar() {
    const sessionStr = sessionStorage.getItem('user');
    const localStr = localStorage.getItem('user');
    const userStr = sessionStr || localStr;
    
    let user = null;
    if (userStr) {
        try {
            user = JSON.parse(userStr);
        } catch (e) {
            console.error('Failed to parse user data:', e);
        }
    }
    
    if (!user || (user.roleName !== 'STAFF' && user.roleId !== 2)) {
        // Not a STAFF user, redirect to login
        window.location.href = '../user/Authenticate.html';
        return;
    }

    // Update staff info
    const staffNameEl = document.getElementById('staffName');
    const staffPositionEl = document.getElementById('staffPosition');
    const staffAvatarEl = document.getElementById('staffAvatar');

    if (staffNameEl && user.fullName) {
        staffNameEl.textContent = user.fullName;
    }
    
    if (staffPositionEl) {
        staffPositionEl.textContent = user.roleName || 'Staff';
    }
    
    if (staffAvatarEl) {
        if (user.avatar) {
            staffAvatarEl.style.backgroundImage = `url('${user.avatar}')`;
        } else {
            staffAvatarEl.style.backgroundImage = "url('../images/default-avatar.png')";
        }
        staffAvatarEl.onerror = function() {
            this.style.backgroundImage = "url('../images/default-avatar.png')";
        };
    }

    // Load unread notification count
    const userId = user.userId || localStorage.getItem('userId');
    if (userId) {
        loadUnreadNotificationCount(userId);
    }

    // Get current page from the document location
    // This will work when sidebar is loaded via fetch
    const currentPage = window.location.pathname.split('/').pop() || 'dashboard.html';
    currentPageName = currentPage;
    
    console.log('Current page:', currentPageName);
    console.log('All nav links:', document.querySelectorAll('aside nav a').length);
    
    highlightActivePage();
}

// Load unread notification count from API
async function loadUnreadNotificationCount(userId) {
    try {
        // Use global API_BASE from config.js, fallback to window.location if needed
        const apiUrl = typeof API_BASE !== 'undefined' ? API_BASE : (window.location.port 
            ? `${window.location.protocol}//${window.location.hostname}:${window.location.port}/api`
            : `${window.location.protocol}//${window.location.hostname}/api`);
        
        const response = await fetch(`${apiUrl}/notification/unread/${userId}`);
        const data = await response.json();
        
        const badge = document.getElementById('notificationBadge');
        if (badge) {
            const count = data.unreadCount || 0;
            badge.textContent = count > 0 ? count : '0';
            // Hide badge if no unread notifications
            if (count === 0) {
                badge.style.display = 'none';
            } else {
                badge.style.display = 'flex';
            }
        }
    } catch (error) {
        console.error('Error loading notification count:', error);
        const badge = document.getElementById('notificationBadge');
        if (badge) {
            badge.textContent = '0';
        }
    }
}

function logoutStaff() {
    // Clear all user data
    localStorage.removeItem('user');
    localStorage.removeItem('role');
    localStorage.removeItem('userId');
    localStorage.removeItem('staffId');
    sessionStorage.clear();
    
    // Redirect to Authenticate page
    window.location.href = '../user/Authenticate.html';
}
