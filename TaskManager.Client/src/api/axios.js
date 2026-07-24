import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5097'
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config; // ✅ THIS LINE FIXES EVERYTHING
});

export default api;