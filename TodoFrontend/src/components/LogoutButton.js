import React from "react";
import { useAuth } from '../auth/AuthContext';

const LogoutButton = () => {
    const {logout} = useAuth();
    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
           logout();
        } catch (error) {
            console.error("Logout error:", error);
            alert("An error occurred during logout.");
        }
    };

    return (
        <button onClick={handleSubmit}>Logout</button>
    );
}

export default LogoutButton;
