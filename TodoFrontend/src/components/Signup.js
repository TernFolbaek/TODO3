import React, { useState } from 'react';
import { useAuth } from '../auth/AuthContext';

const Signup = () => {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const { login } = useAuth(); // Use login to authenticate user right after signup

    const handleSubmit = async (event) => {
        event.preventDefault();
        const response = await fetch('https://localhost:7060/signup', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            const errorData = await response.json();
            console.error("Signup error:", errorData.error || "Unknown error during signup.");
            alert(errorData.error || 'Signup failed');
            return;
        }

        const data = await response.json();
        console.log("Signup successful, received data:", data);
        login(data.accessToken, data.refreshToken);
    };




    return (
        <form onSubmit={handleSubmit}>
            <input
                type="text"
                placeholder="Username"
                value={username}
                onChange={e => setUsername(e.target.value)}
            />
            <input
                type="password"
                placeholder="Password"
                value={password}
                onChange={e => setPassword(e.target.value)}
            />
            <button type="submit">Sign Up</button>
        </form>
    );
};

export default Signup;
