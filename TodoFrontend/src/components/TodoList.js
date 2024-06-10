import React, { useState, useEffect, useRef } from 'react';
import { HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';

const TodoList = () => {
    const [todos, setTodos] = useState([]);
    const hubConnectionRef = useRef(null); // Using useRef to persist the hub connection

    // Fetch Todos on component mount
    useEffect(() => {
        const fetchTodos = async () => {
            const token = localStorage.getItem('authToken');
            const response = await fetch('https://localhost:7060/api/todo', {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (!response.ok) {
                console.error(`Failed to fetch todos: ${response.statusText}`);
                return;
            }
            const data = await response.json();
            setTodos(data);
        };

        fetchTodos();
    }, []);

    useEffect(() => {
        const token = localStorage.getItem('authToken');
        if (!token) {
            console.error("No auth token available.");
            return;
        }

        if (hubConnectionRef.current && hubConnectionRef.current.state !== HubConnectionState.Disconnected) {
            return;
        }

        const connection = new HubConnectionBuilder()
            .withUrl('https://localhost:7060/todoHub', { accessTokenFactory: () => token })
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveTodoStatusUpdate', (todoId, status) => {
            setTodos(currentTodos => currentTodos.map(todo =>
                todo.id === todoId ? { ...todo, isComplete: status === 'Completed', status: status } : todo
            ));
        });

        connection.on('ReceiveTodoDueDateUpdate', (todoId, dueDate) => {
            setTodos(currentTodos => currentTodos.map(todo =>
                todo.id === todoId ? { ...todo, dueDate: new Date(dueDate) } : todo
            ));
        });

        connection.onclose(() => {
            console.log('SignalR connection closed');
            hubConnectionRef.current = null;
        });

        const startConnection = async () => {
            try {
                await connection.start();
                console.log('SignalR Connected.');
                hubConnectionRef.current = connection;
            } catch (err) {
                console.error('Error establishing SignalR connection:', err);
            }
        };

        startConnection();

        return () => {
            connection.stop();
        };
    }, []);

    const handleDueDateChange = async (todoId, date) => {
        const token = localStorage.getItem('authToken');
        const response = await fetch(`https://localhost:7060/api/todo/updateDueDate/${todoId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(date)
        });

        if (!response.ok) {
            console.error(`Failed to update due date: ${response.statusText}`);
        }
    };

    const toggleTodoCompletion = async (todoId, isComplete) => {
        try {
            const response = await fetch(`https://localhost:7060/api/todo/toggleCompletion/${todoId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({isComplete: !isComplete})
            });
            if (!response.ok) {
                throw new Error(`Failed to toggle todo completion: ${response.statusText}`);
            }
            const updatedTodo = await response.json();
            setTodos(currentTodos => currentTodos.map(todo =>
                todo.id === updatedTodo.id ? {...todo, ...updatedTodo} : todo
            ));
        } catch (error) {
            console.error("Error toggling todo completion: ", error);
            alert("Failed to update todo status.");
        }
    };

    return (
        <div>
            <h1>Todo List</h1>
            {todos.length > 0 ? (
                <ul>
                    {todos.map(todo => (
                        <li key={todo.id}>
                            <strong>Description:</strong> {todo.description} <br/>
                            <strong>Status:</strong> {todo.status} <br/>
                            <strong>Is Complete:</strong> {todo.isComplete ? 'Completed' : 'Not Completed'} <br/>
                            <strong>Due Date:</strong>
                            <DatePicker
                                selected={todo.dueDate ? new Date(todo.dueDate) : null}
                                onChange={(date) => handleDueDateChange(todo.id, date)}
                                dateFormat="MMMM d, yyyy"
                                isClearable
                            />
                            <br/>
                            <button onClick={() => toggleTodoCompletion(todo.id, todo.isComplete)}>
                                {todo.isComplete ? 'Mark as Pending' : 'Mark as Completed'}
                            </button>
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





