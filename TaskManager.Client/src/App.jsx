import { Routes, Route } from 'react-router-dom';
import Login from './pages/Login.jsx';
import Tasks from './pages/Tasks.jsx';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element= {<Tasks/>}/>
    </Routes>
  );
}

export default App;