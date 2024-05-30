import React, { createContext, useState, useContext, useEffect } from 'react';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
    const [authToken, setAuthToken] = useState(localStorage.getItem('authToken'));

    const login = (token) => {
        localStorage.setItem('authToken', token);  // Store token in local storage
        setAuthToken(token);
    };

    const logout = () => {
        localStorage.removeItem('authToken');  // Remove token from local storage
        setAuthToken(null);
    };

    // Optional: Automatically log in if token exists in local storage
    useEffect(() => {
        const token = localStorage.getItem('authToken');
        if (token) {
            setAuthToken(token);
        }
    }, []);

    return (
        <AuthContext.Provider value={{ authToken, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);
