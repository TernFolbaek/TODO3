import React, {useState} from "react";

const LogoutButton = () => {

    const handleSubmit = (e) => {
        alert("Logout clicked")
        e.preventDefault();
        localStorage.removeItem("authToken");
        window.location.reload();

    }


    return (
        <button onClick={handleSubmit}>Logout</button>
    )
}

export default LogoutButton;