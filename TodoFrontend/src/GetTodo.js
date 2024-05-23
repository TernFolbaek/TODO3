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
            const response = await fetch(`https://localhost:7060/api/todo/${todoId}?timezone=${tz}`);
            if (!response.ok) throw new Error('Failed to fetch todo');
            const data = await response.json();
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
        const ipToUse = customIP; // Use the custom IP if provided
        const timezone = await fetchTimezone(ipToUse); // Fetch timezone with the entered or detected IP
        if (!timezone) return; // Exit if no timezone could be fetched
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
                    <p>ID: {todo.id}</p>
                    <p>Description: {todo.description}</p>
                    <p>Status: {todo.isComplete ? 'Completed' : 'Pending'}</p>
                    <p>Due Date: {todo.dueDate}</p> {/* Display the date as returned by the backend */}
                </div>
            )}
        </div>
    );
}

export default GetTodo;
