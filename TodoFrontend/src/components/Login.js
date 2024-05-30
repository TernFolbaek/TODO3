// src/components/Login.js
import React, { useState } from 'react';
import { useAuth } from '../auth/AuthContext';

const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const { login } = useAuth();  // Use the login function from AuthContext

    const handleSubmit = async (event) => {
        event.preventDefault();
        const response = await fetch('https://localhost:7060/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });
        const data = await response.json();
        if (response.ok) {
            login(data.token);  // Store the token in local storage via AuthContext
        } else {
            alert(data.error || 'Login failed');
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <input
                type="text"
                placeholder="Username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
            />
            <input
                type="password"
                placeholder="Password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
            />
            <button type="submit">Login</button>
        </form>
    );
};

export default Login;
