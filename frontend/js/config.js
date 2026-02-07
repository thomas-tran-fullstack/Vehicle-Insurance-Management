/**
 * API Configuration
 * Dynamically generates API base URL based on current location
 * Supports local development and ngrok/production deployments
 */

// Prevent multiple declarations by checking if API_BASE is already set
if (typeof window.API_BASE === 'undefined') {
    // Get the protocol, hostname, and port from current window location
    const protocol = window.location.protocol; // http: or https:
    const hostname = window.location.hostname; // localhost, 127.0.0.1, or domain name
    const port = window.location.port; // 5169, 3000, 8080, etc.

    let API_BASE = "";

    // Determine API URL based on hostname
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
        // Local development: use localhost:5169
        API_BASE = `http://localhost:5169/api`;
    } else {
        // Production/Deployment (ngrok, server, etc.)
        // Remove port from URL if it exists and use same protocol
        API_BASE = port 
            ? `${protocol}//${hostname}:${port}/api`
            : `${protocol}//${hostname}/api`;
    }

    // Set to window object for global access
    window.API_BASE = API_BASE;
}

// Fallback (should rarely be used)
const API_BASE_FALLBACK = "http://localhost:5169/api";

// Log for debugging (development only)
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    console.log(`[✓ Config] API Base: ${API_BASE}`);
    console.log(`[✓ Config] Origin: ${window.location.origin}`);
}

// For pages in subdirectories that need to reference config.js
// auto-assign API_BASE if not already defined
if (typeof window.API_BASE === 'undefined') {
    window.API_BASE = API_BASE;
}
if (typeof window.API_BASE_FALLBACK === 'undefined') {
    window.API_BASE_FALLBACK = API_BASE_FALLBACK;
}
