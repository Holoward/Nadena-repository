import axios from 'axios';

const isLocalHost = typeof window !== 'undefined'
    && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

const configuredApiUrl = process.env.REACT_APP_API_URL;
const apiBaseUrl = (isLocalHost ? 'http://localhost:5000' : configuredApiUrl || 'http://localhost:5000').replace(/\/+$/, '');

axios.defaults.baseURL = apiBaseUrl;

if (typeof window !== 'undefined' && !window.__NADENA_FETCH_PATCHED__) {
    const originalFetch = window.fetch.bind(window);
    window.fetch = (input, init) => {
        if (typeof input === 'string' && input.startsWith('/api/')) {
            return originalFetch(`${apiBaseUrl}${input}`, init);
        }

        return originalFetch(input, init);
    };

    window.__NADENA_FETCH_PATCHED__ = true;
}

export default axios;
