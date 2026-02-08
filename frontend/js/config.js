/**
 * API Configuration
 * Dynamically generates API base URL based on current location
 * Supports local development and ngrok/production deployments
 * 
 * IMPORTANT NOTES:
 * - For localhost: Frontend and Backend run on localhost (http://localhost:5169)
 * - For ngrok: Both Frontend and Backend must run through ngrok tunnels
 *   Example:
 *   1. Start backend: ngrok http 5169
 *   2. Copy ngrok URL and use it for MANUAL_NGROK_API_URL below
 *   3. Frontend will be served through ngrok or another tunnel
 */

// MANUAL CONFIGURATION (override automatic detection if needed)
// Change this if automatic detection doesn't work
const MANUAL_NGROK_API_URL = null; // e.g., 'https://your-backend-ngrok.ngrok-free.dev/api'

// Prevent multiple declarations by checking if API_BASE is already set
if (typeof window.API_BASE === 'undefined') {
    let API_BASE = "";

    // Use manual configuration if provided
    if (MANUAL_NGROK_API_URL) {
        API_BASE = MANUAL_NGROK_API_URL;
    } else {
        // Get the protocol, hostname, and port from current window location
        const protocol = window.location.protocol; // http: or https:
        const hostname = window.location.hostname; // localhost, 127.0.0.1, or domain name
        const port = window.location.port; // 5169, 3000, 8080, etc.

        // Determine API URL based on hostname
        if (hostname === 'localhost' || hostname === '127.0.0.1') {
            // Local development: use localhost:5169
            API_BASE = `http://localhost:5169/api`;
        } else if (hostname.includes('ngrok')) {
            // If frontend is on ngrok, assume backend is also on ngrok with same domain pattern
            // This needs manual configuration - set MANUAL_NGROK_API_URL above
            API_BASE = `${protocol}//${hostname}/api`;
        } else {
            // Production/Deployment (server, etc.)
            // Use same protocol and host
            API_BASE = port 
                ? `${protocol}//${hostname}:${port}/api`
                : `${protocol}//${hostname}/api`;
        }
    }

    // Set to window object for global access
    window.API_BASE = API_BASE;
    console.log('🔧 API_BASE configured:', API_BASE);
}

// Fallback (should rarely be used)
if (typeof window.API_BASE_FALLBACK === 'undefined') {
    window.API_BASE_FALLBACK = "http://localhost:5169/api";
}
