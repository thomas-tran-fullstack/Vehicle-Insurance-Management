let currentPageName = '';

function setCurrentPage(pageName) {
    currentPageName = pageName;
    highlightActivePage();
}

function highlightActivePage() {
    if (!currentPageName) {
        currentPageName = window.location.pathname.split('/').pop() || 'dashboard.html';
    }
    
    // Wait a moment for sidebar to be available if not yet
    setTimeout(() => {
        const navLinks = document.querySelectorAll('aside nav a');
        if (navLinks.length === 0) {
            // Sidebar not loaded yet, try again
            setTimeout(highlightActivePage, 100);
            return;
        }
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            link.classList.remove('active-nav-item');
            
            // Compare filenames case-insensitively
            if (href && href.toLowerCase() === currentPageName.toLowerCase()) {
                link.classList.add('active-nav-item');
            }
        });
    }, 50);
}

function initializeAdminSidebar() {
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
    
    if (!user || (user.roleName !== 'ADMIN' && user.roleId !== 1)) {
        // Not an ADMIN user, redirect to login
        window.location.href = '../user/Authenticate.html';
        return;
    }

    // Update admin info
    const adminNameEl = document.getElementById('adminName');
    const adminAvatarEl = document.getElementById('adminAvatar');

    if (adminNameEl && user.username) {
        adminNameEl.textContent = user.username;
    }
    
    if (adminAvatarEl) {
        // For admin users, always use admin.png
        adminAvatarEl.style.backgroundImage = "url('../images/admin.png')";
        adminAvatarEl.onerror = function() {
            this.style.backgroundImage = "url('../images/default-avatar.png')";
        };
    }

    // Get current page from the document location
    const currentPage = window.location.pathname.split('/').pop() || 'dashboard.html';
    currentPageName = currentPage;
    
    console.log('Current page:', currentPageName);
    
    // Highlight after a short delay to ensure sidebar is fully loaded
    setTimeout(() => {
        console.log('Highlighting page:', currentPageName);
        highlightActivePage();
    }, 150);
}

function logoutAdmin() {
    // Clear all user data
    localStorage.removeItem('user');
    localStorage.removeItem('role');
    localStorage.removeItem('userId');
    localStorage.removeItem('staffId');
    sessionStorage.clear();
    
    // Redirect to Authenticate page
    window.location.href = '../user/Authenticate.html';
}
