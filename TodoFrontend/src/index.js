import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import CreateTodo from './CreateTodo';
import GetTodo from './GetTodo';

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
      <CreateTodo />
      <GetTodo />
  </React.StrictMode>
);