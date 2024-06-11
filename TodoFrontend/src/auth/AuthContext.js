import React, { createContext, useState, useContext, useEffect } from 'react';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [authToken, setAuthToken] = useState(localStorage.getItem('authToken'));
    const [refreshToken, setRefreshToken] = useState(localStorage.getItem('refreshToken'));

    useEffect(() => {
        // Setup all fetch requests to use the authToken if available
        const defaultHeaders = {
            'Authorization': authToken ? `Bearer ${authToken}` : '',
            'Content-Type': 'application/json'
        };

        fetch.defaults = { headers: defaultHeaders };
    }, [authToken]);

    const login = (authToken, refreshToken) => {
        console.log('Storing tokens:', { authToken, refreshToken });
        localStorage.setItem('authToken', authToken);
        localStorage.setItem('refreshToken', refreshToken);
        setAuthToken(authToken);
        setRefreshToken(refreshToken);
    };

    const logout = () => {
        console.log('Logging out');
        localStorage.removeItem('authToken');
        localStorage.removeItem('refreshToken');
        setAuthToken(null);
        setRefreshToken(null);
    };

    const refreshAccessToken = async () => {
        try {
            console.log('Attempting to refresh access token with refresh token:', refreshToken);
            const response = await fetch('https://localhost:7060/refresh-token', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken })
            });
            const data = await response.json();
            if (response.ok) {
                console.log('Received new tokens:', data);
                login(data.accessToken, data.refreshToken);
            } else {
                console.error('Failed to refresh token:', data);
                logout();
            }
        } catch (error) {
            console.error('Error refreshing access token:', error);
            logout();
        }
    };

    useEffect(() => {
        const interval = setInterval(() => {
            if (authToken) {
                refreshAccessToken();
            }
        }, 1000 * 60 * 28);
        return () => clearInterval(interval);
    }, [authToken]);

    return (
        <AuthContext.Provider value={{ authToken, refreshToken, login, logout, refreshAccessToken }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);
