import api from './axios';

export const login = async (email,password)=>{
    const res = await api.post('/api/Auth/login',{email,password});
    localStorage.setItem('accessToken', res.data.accessToken);
    localStorage.setItem('refreshToken',res.data.refreshToken);
    return res.data;
};

export const logout = async ()=>{
    await api.post('api/Auth/logout');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
};