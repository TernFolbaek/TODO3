// src/components/TodoList.js
import React, { useState, useEffect } from 'react';

const TodoList = () => {
    const [todos, setTodos] = useState([]);

    useEffect(() => {
        const fetchTodos = async () => {
            try {
                const response = await fetch('https://localhost:7060/api/todo', {
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
                        'Content-Type': 'application/json'
                    }
                });
                if (!response.ok) {
                    throw new Error(`Failed to fetch todos: ${response.statusText}`);
                }
                const data = await response.json();
                setTodos(data);
            } catch (error) {
                console.error("Error fetching data: ", error);
                alert("Error fetching todos.");
            }
        };

        fetchTodos();
    }, []);

    return (
        <div>
            <h1>Todo List</h1>
            {todos.length > 0 ? (
                <ul>
                    {todos.map(todo => (
                        <li key={todo.id}>
                            <strong>Description:</strong> {todo.description} <br />
                            <strong>Status:</strong> {todo.isComplete ? 'Completed' : 'Pending'} <br />
                            <strong>Due Date:</strong> {todo.dueDate ? new Date(todo.dueDate).toLocaleDateString() : 'No due date'}
                        </li>
                    ))}
                </ul>
            ) : (
                <p>No todos found!</p>
            )}
        </div>
    );
};

export default TodoList;
