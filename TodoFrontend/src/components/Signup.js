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
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        const data = await response.json();
        if (response.ok) {
            login(data.token); // This will store the token and update the auth state
            console.log("Received token:", data.token);

        } else {
            alert(data.error || 'Signup failed');
        }
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
