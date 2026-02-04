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
        link.classList.add('text-slate-600', 'dark:text-slate-400');
        
        // Remove the vertical line indicator
        const existingLine = link.querySelector('.ml-auto');
        if (existingLine && existingLine.classList.contains('w-1')) {
            existingLine.remove();
        }
        
        if (href === currentPageName) {
            link.classList.remove('text-slate-600', 'dark:text-slate-400');
            link.classList.add('bg-primary/10', 'text-primary');
            
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

    // Get current page from the document location
    // This will work when sidebar is loaded via fetch
    const currentPage = window.location.pathname.split('/').pop() || 'dashboard.html';
    currentPageName = currentPage;
    
    console.log('Current page:', currentPageName);
    console.log('All nav links:', document.querySelectorAll('aside nav a').length);
    
    highlightActivePage();
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
