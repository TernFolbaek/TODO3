import React, { useState } from 'react';
import { useAuth } from './auth/AuthContext';
import Login from './components/Login';
import Signup from './components/Signup';
import CreateTodo from './CreateTodo';
import GetTodo from './GetTodo';

const App = () => {
    const { authToken } = useAuth();
    const [showLogin, setShowLogin] = useState(true); // Toggle between Login and Signup

    return (
        <div>
            {!authToken ? (
                <div>
                    <button onClick={() => setShowLogin(true)}>Login</button>
                    <button onClick={() => setShowLogin(false)}>Sign Up</button>
                    {showLogin ? <Login /> : <Signup />}
                </div>
            ) : (
                <>
                    <CreateTodo />
                    <GetTodo />
                </>
            )}
        </div>
    );
};

export default App;
