import React, { useState, useEffect } from 'react';

function CreateTodo() {
    const [description, setDescription] = useState('');
    const [isComplete, setIsComplete] = useState(false);
    const [dueDate, setDueDate] = useState('');
    const [users, setUsers] = useState([]);
    const [selectedUserIds, setSelectedUserIds] = useState([]);

    useEffect(() => {
        const fetchUsers = async () => {
            try {
                const response = await fetch('https://localhost:7060/users');
                if (response.ok) {
                    const data = await response.json();
                    console.log("Fetched Users:", data); // Log the data to verify it
                    setUsers(data);
                } else {
                    console.error('Failed to fetch users, Status:', response.status);
                }
            } catch (error) {
                console.error('Error fetching users:', error);
            }
        };
        fetchUsers();
    }, []);


    const handleSubmit = async (e) => {
        e.preventDefault();

        const todo = {
            description,
            isComplete,
            dueDate: dueDate ? new Date(dueDate).toISOString() : null,
            usernames: selectedUserIds.map(id => users.find(user => user.id === id)?.username)
        };
        const response = await fetch('https://localhost:7060/api/todo', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(todo),
            credentials: 'include'
        });
        if (response.ok) {
            console.log("Todo created successfully");
            setDescription('');
            setIsComplete(false);
            setDueDate('');
            setSelectedUserIds([]);
        } else {
            const errorText = await response.text();
            console.error('Failed to create todo. Server responded with:', errorText);
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
            <label>
                Completed:
                <input
                    type="checkbox"
                    checked={isComplete}
                    onChange={e => setIsComplete(e.target.checked)}
                />
            </label>
            <input
                type="date"
                value={dueDate}
                onChange={e => setDueDate(e.target.value)}
                required={false}
            />
            <label>
                Assign to:
                <select
                    multiple
                    value={selectedUserIds}
                    onChange={e => setSelectedUserIds([...e.target.selectedOptions].map(o => Number(o.value)))}
                    style={{
                        width: '100%',
                        height: '100px',
                        backgroundColor: 'white',
                        color: 'black'
                    }}
                >
                    {users.map(user => (
                        <option key={user.id} value={user.id}>{user.username}</option>
                    ))}
                </select>

            </label>
            <p>{selectedUserIds}</p>
            <button type="submit">Create Todo</button>
        </form>
    );
}

export default CreateTodo;
