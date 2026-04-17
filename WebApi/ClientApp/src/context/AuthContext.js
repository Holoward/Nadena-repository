import React, { createContext, useState, useContext, useEffect } from 'react';
import { jwtDecode } from 'jwt-decode';
import { clearStoredToken, getStoredToken, setStoredToken } from '../utils/authStorage';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [auth, setAuth] = useState({
        token: getStoredToken(),
        user: null,
        role: null,
        isAuthenticated: false
    });

    useEffect(() => {
        const token = getStoredToken();
        if (token) {
            try {
                const decoded = jwtDecode(token);
                const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                    decoded.role ||
                    decoded['role'];

                setAuth({
                    token,
                    user: decoded,
                    role: role,
                    isAuthenticated: true
                });
            } catch (error) {
                console.error('Error decoding token:', error);
                logout();
            }
        }
    }, []);

    const login = (token) => {
        setStoredToken(token);
        try {
            const decoded = jwtDecode(token);
            const role = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                decoded.role ||
                decoded['role'];

            setAuth({
                token,
                user: decoded,
                role: role,
                isAuthenticated: true
            });
            return { success: true, role };
        } catch (error) {
            console.error('Error decoding token:', error);
            logout();
            return { success: false, error: 'Invalid token' };
        }
    };

    const logout = () => {
        clearStoredToken();
        setAuth({
            token: null,
            user: null,
            role: null,
            isAuthenticated: false
        });
    };

    const hasRole = (requiredRole) => {
        return auth.role === requiredRole;
    };

    return (
        <AuthContext.Provider value={{ auth, login, logout, hasRole }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

export default AuthContext;
