import React, { createContext, useState, useContext, useEffect } from 'react';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [authToken, setAuthToken] = useState(localStorage.getItem('authToken'));
    const [refreshToken, setRefreshToken] = useState(localStorage.getItem('refreshToken'));

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

    // Add a method to refresh the access token using the refresh token
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
                login(data.accessToken, data.refreshToken); // Use the new refreshToken if the server sends a new one
            } else {
                console.error('Failed to refresh token:', data);
                logout();
            }
        } catch (error) {
            console.error('Error refreshing access token:', error);
            logout();
        }
    };


    // Auto-refresh token on expiry
    useEffect(() => {
        const interval = setInterval(() => {
            if (authToken) {
                refreshAccessToken();
            }
        }, 1000 * 60 * 30); //every 30 minutes WUHUUUU
        return () => clearInterval(interval);
    }, [authToken]);

    return (
        <AuthContext.Provider value={{ authToken, login, logout, refreshAccessToken }}>
            {children}
        </AuthContext.Provider>
    );
};


export const useAuth = () => useContext(AuthContext);
