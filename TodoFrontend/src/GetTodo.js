import React, { useState } from 'react';

function GetTodo() {
    const [id, setId] = useState('');
    const [customIP, setCustomIP] = useState(''); // For manual IP entry
    const [todo, setTodo] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    const fetchTimezone = async (ipAddress) => {
        setLoading(true);
        try {
            const apiKey = '2550775a64b2418f80c1986e638c708f';
            const url = `https://api.ipgeolocation.io/timezone?apiKey=${apiKey}&ip=${ipAddress}`;
            const response = await fetch(url);
            if (!response.ok) throw new Error('Failed to retrieve timezone information');
            const { timezone } = await response.json();
            return timezone;
        } catch (err) {
            setError(err.message);
            setLoading(false);
            return null;
        }
    };

    const fetchTodoItem = async (todoId, tz) => {
        try {
            const response = await fetch(`https://localhost:7060/api/todo/${todoId}?timezone=${tz}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
                credentials: 'include'

            });

            if (!response.ok) throw new Error('Failed to fetch todo');
            const data = await response.json();
            console.log("Fetched Todo:", data);
            setTodo(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    const handleFetch = async () => {
        if (!id) {
            setError("Please enter a valid Todo ID");
            return;
        }
        setError('');
        const ipToUse = customIP;
        const timezone = await fetchTimezone(ipToUse);
        if (!timezone) return;
        fetchTodoItem(id, timezone);
    };

    return (
        <div>
            <input type="text" value={customIP} onChange={(e) => setCustomIP(e.target.value)} placeholder="Enter custom IP (optional)" />
            <input type="text" value={id} onChange={(e) => setId(e.target.value)} placeholder="Enter Todo ID" />
            <button onClick={handleFetch} disabled={!id}>Fetch Todo</button>
            {loading && <p>Loading...</p>}
            {error && <p>Error: {error}</p>}
            {todo && (
                <div>
                    <h3>Todo Details</h3>
                    <p>ID: {todo.todoId}</p>
                    <p>Description: {todo.description}</p>
                    <p>Status: {todo.isComplete ? 'Completed' : 'Pending'}</p>
                    <p>Due Date: {todo.dueDate}</p>
                    {todo.assignedUsers && todo.assignedUsers.length > 0 && (
                        <div>
                            <p>Assigned Users:</p>
                            {todo.assignedUsers.map((user) => (
                                <p key={user.userId}>{user.username}</p>
                            ))}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

export default GetTodo;
