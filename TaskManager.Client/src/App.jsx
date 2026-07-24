import { Routes, Route } from 'react-router-dom';
import Login from './pages/Login.jsx';
import Tasks from './pages/Tasks.jsx';
import Register from './pages/Register.jsx';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element= {<Tasks/>}/>
      <Route path="/register" element={<Register />} />
    </Routes>
  );
}

export default App;