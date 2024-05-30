// src/App.js
import React from 'react';
import { useAuth } from './auth/AuthContext';
import Login from './components/Login'; // Assume you have a Login component
import CreateTodo from './CreateTodo';
import GetTodo from './GetTodo';

const App = () => {
    const { authToken } = useAuth();

    return (
        <div>
            {!authToken ? (
                <Login />
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
