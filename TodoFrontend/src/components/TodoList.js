import React, { useState, useEffect } from 'react';
import { HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import DatePicker from 'react-datepicker';
import 'react-datepicker/dist/react-datepicker.css';

const TodoList = () => {
    const [todos, setTodos] = useState([]);
    const [hubConnection, setHubConnection] = useState(null);

    const setupSignalRConnection = async (token) => {
        const connection = new HubConnectionBuilder()
            .withUrl('https://localhost:7060/todoHub', {
                accessTokenFactory: () => token
            })
            .configureLogging(LogLevel.Information)
            .withAutomaticReconnect([0, 2000, 10000, 30000]) // Will try reconnecting at these intervals.
            .build();

        connection.onreconnecting((error) => {
            console.error(`Connection lost due to error "${error}". Reconnecting.`);
        });

        connection.onreconnected((connectionId) => {
            console.log(`Connection reestablished. Connected with connectionId "${connectionId}".`);
        });

        connection.onclose((error) => {
            console.error(`Connection closed. Error: ${error}`);
            console.error('Attempting to manually reconnect...');
            setTimeout(() => setupSignalRConnection(localStorage.getItem('authToken')), 5000); // Attempt to reconnect in 5 seconds
        });

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

        try {
            await connection.start();
            console.log('SignalR Connected.');
            setHubConnection(connection);
        } catch (err) {
            console.error('Error establishing SignalR connection:', err);
        }
    };

    useEffect(() => {
        if (!hubConnection) {
            const token = localStorage.getItem('authToken');
            setupSignalRConnection(token);
        }

        return () => {
            hubConnection?.stop();
        };
    }, [hubConnection]);

    const handleDueDateChange = async (todoId, date) => {
        try {
            const response = await fetch(`https://localhost:7060/api/todo/updateDueDate/${todoId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(date)
            });
            if (!response.ok) {
                throw new Error(`Failed to update due date: ${response.statusText}`);
            }
        } catch (error) {
            console.error("Error updating due date: ", error);
            alert("Failed to update due date.");
        }
    };

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

    const toggleTodoCompletion = async (todoId, isComplete) => {
        try {
            const response = await fetch(`https://localhost:7060/api/todo/toggleCompletion/${todoId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ isComplete: !isComplete })
            });
            if (!response.ok) {
                throw new Error(`Failed to toggle todo completion: ${response.statusText}`);
            }
            const updatedTodo = await response.json();
            setTodos(currentTodos => currentTodos.map(todo =>
                todo.id === updatedTodo.id ? { ...todo, ...updatedTodo } : todo
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
                            <strong>is Complete:</strong> {todo.isComplete ? 'completed' : 'not completed'} <br/>
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

