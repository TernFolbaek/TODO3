import React, { createContext, useContext, useState, useEffect } from 'react';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [isLoggedIn, setIsLoggedIn] = useState(false);

    const login = async (username, password) => {
        const response = await fetch('https://localhost:7060/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include', // Ensure cookies are sent and received
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            setIsLoggedIn(true);
            console.log('Login successful');
        } else {
            console.error('Login failed:', await response.json());
        }
    };

    const logout = async () => {
        const response = await fetch('https://localhost:7060/logout', {
            method: 'POST',
            credentials: 'include',
        });
        if (response.ok) {
            setIsLoggedIn(false);
            console.log('Logout successful');
        } else {
            console.error('Logout failed:', await response.json());
        }
    };

    useEffect(() => {
        const checkSession = async () => {
            const response = await fetch('https://localhost:7060/check-session', {
                credentials: 'include'
            });
            if (response.ok) {
                setIsLoggedIn(true);
                console.log('Session active');
            } else {
                setIsLoggedIn(false);
                console.log('No active session');
            }
        };
        checkSession();
    }, []);

    return (
        <AuthContext.Provider value={{ isLoggedIn, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);
