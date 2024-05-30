import React, { createContext, useState, useContext, useEffect } from 'react';

const AuthContext = createContext(null);

// In your AuthContext
export const AuthProvider = ({ children }) => {
    const [authToken, setAuthToken] = useState(localStorage.getItem('authToken'));

    const login = (token) => {
        localStorage.setItem('authToken', token);
        setAuthToken(token);
    };

    const logout = () => {
        localStorage.removeItem('authToken'); // Remove the token from local storage
        setAuthToken(null);
    };

    return (
        <AuthContext.Provider value={{ authToken, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};


export const useAuth = () => useContext(AuthContext);
