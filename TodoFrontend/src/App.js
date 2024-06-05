// src/App.js
import React from 'react';
import { useAuth } from './auth/AuthContext';
import Login from './components/Login';
import Signup from './components/Signup';
import CreateTodo from './CreateTodo';
import GetTodo from './GetTodo';
import TodoList from './components/TodoList'; // Import the TodoList component
import LogoutButton from './components/LogoutButton';

const App = () => {
    const { authToken } = useAuth();

    return (
        <div>
            {!authToken ? (
                <div>
                    <Login />
                    <Signup />
                </div>
            ) : (
                <>
                    <LogoutButton/>
                    <CreateTodo />
                    <GetTodo />
                    <TodoList />
                </>
            )}
        </div>
    );
};

export default App;
