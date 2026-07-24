import api from "./axios"
export const getTasks = () => api.get("/api/Tasks");
