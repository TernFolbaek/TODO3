// src/index.js
import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import { AuthProvider } from './auth/AuthContext';
import App from './App';

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
    <React.StrictMode>
        <AuthProvider>
            <App />
        </AuthProvider>
    </React.StrictMode>
);
