import React, { useState } from 'react';

function CreateTodo() {
    const [description, setDescription] = useState('');
    const [isComplete, setIsComplete] = useState(false);
    const [dueDate, setDueDate] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        const todo = {
            description: description,
            isComplete: isComplete,
            dueDate: new Date(dueDate).toISOString() // Converts to a string in simplified extended ISO format (ISO 8601), which is always in UTC
        };
        const response = await fetch('https://localhost:7060/api/todo', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(todo),
        });
        if (response.ok) {
            console.log("Todo created successfully");
            setDescription('');
            setIsComplete(false);
            setDueDate('');
        }
        else
        {
            const errorText = await response.text();
            throw new Error('Failed to create todo. Server responded with: ' + errorText);
        }

    };

    return (
        <form onSubmit={handleSubmit}>
            <input
                type="text"
                value={description}
                onChange={e => setDescription(e.target.value)}
                placeholder="Description"
                required
            />
            <input
                type="checkbox"
                checked={isComplete}
                onChange={e => setIsComplete(e.target.checked)}
            />
            <input
                type="date"
                value={dueDate}
                onChange={e => setDueDate(e.target.value)}
                required
            />
            <button type="submit">Create Todo</button>
        </form>
    );
}

export default CreateTodo;
