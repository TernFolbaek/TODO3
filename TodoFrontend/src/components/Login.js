// src/components/Login.js
import React, { useState } from 'react';
import { useAuth } from '../auth/AuthContext';

const Login = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const { login } = useAuth();  // Use the login function from AuthContext

    const handleSubmit = async (event) => {
        event.preventDefault();
        console.log("Attempting login for:", username); // Log username attempting to login

        try {
            const response = await fetch('https://localhost:7060/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                console.error("Login request failed with status:", response.status);
                const errorData = await response.json(); // Properly parse error messages
                console.error("Login failed with error:", errorData.error || "Unknown error");
                alert(errorData.error || 'Login failed due to server error');
                return;
            }

            const data = await response.json();
            console.log("Login successful, received data:", data); // Log success and token data

            if (data.accessToken && data.refreshToken) {
                login(data.accessToken, data.refreshToken);  // Store both tokens in local storage via AuthContext
            } else {
                console.error("Received data is missing the accessToken or refreshToken:", data);
                alert("Login failed due to missing tokens.");
            }
        } catch (error) {
            console.error("Error during the login process:", error);
            alert("An error occurred while logging in.");
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
