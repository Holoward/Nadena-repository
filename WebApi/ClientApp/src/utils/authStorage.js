export const TOKEN_STORAGE_KEY = 'nadena_token';

export const getStoredToken = () => localStorage.getItem(TOKEN_STORAGE_KEY);

export const setStoredToken = (token) => {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
};

export const clearStoredToken = () => {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
};
