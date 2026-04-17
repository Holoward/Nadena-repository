import axios from 'axios';
import * as SecureStore from 'expo-secure-store';
import Constants from 'expo-constants';
import { resetToLogin } from '../navigation/navigationRef';

const TOKEN_KEY = 'nadena_token';

const getBaseUrl = () => {
  // Production
  if (process.env.EXPO_PUBLIC_API_URL) {
    return process.env.EXPO_PUBLIC_API_URL;
  }
  // Local dev: use the machine's LAN IP, not localhost
  // Replace with your machine's actual LAN IP when testing on device
  return 'http://192.168.1.X:5034/api/v1';
};

const BASE_URL = getBaseUrl();

// Create axios instance
const apiClient = axios.create({
    baseURL: BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Request interceptor to add JWT token
apiClient.interceptors.request.use(
    async (config) => {
        try {
            const token = await SecureStore.getItemAsync(TOKEN_KEY);
            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
        } catch (error) {
            console.error('Error reading token from SecureStore:', error);
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor for 401 handling
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
    failedQueue.forEach((prom) => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });
    failedQueue = [];
};

apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                    .then((token) => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return apiClient(originalRequest);
                    })
                    .catch((err) => {
                        return Promise.reject(err);
                    });
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                // Clear the token
                await SecureStore.deleteItemAsync(TOKEN_KEY);

                // Redirect to login screen
                resetToLogin();

                processQueue(error, null);

                return Promise.reject(error);
            } catch (err) {
                processQueue(err, null);
                return Promise.reject(err);
            } finally {
                isRefreshing = false;
            }
        }

        return Promise.reject(error);
    }
);

// Auth functions
export const login = async (email, password) => {
    const response = await apiClient.post('/Auth/login', { email, password });
    if (response.data?.token) {
        await SecureStore.setItemAsync(TOKEN_KEY, response.data.token);
    }
    return response.data;
};

export const register = async (email, password, firstName, lastName, role) => {
    const response = await apiClient.post('/Auth/register', {
        email,
        password,
        firstName,
        lastName,
        role,
    });
    if (response.data?.token) {
        await SecureStore.setItemAsync(TOKEN_KEY, response.data.token);
    }
    return response.data;
};

export const logout = async () => {
    await SecureStore.deleteItemAsync(TOKEN_KEY);
};

export const getToken = async () => {
    return await SecureStore.getItemAsync(TOKEN_KEY);
};

export const hasToken = async () => {
    const token = await SecureStore.getItemAsync(TOKEN_KEY);
    return !!token;
};

// Volunteer API
export const getVolunteerByUserId = async (userId) => {
    const response = await apiClient.get(`/Volunteer/user/${userId}`);
    return response.data;
};

export const uploadFile = async (formData, onProgress) => {
    const response = await apiClient.post('/Volunteer/upload-file', formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
            const percentCompleted = Math.round(
                (progressEvent.loaded * 100) / progressEvent.total
            );
            if (onProgress) {
                onProgress(percentCompleted);
            }
        },
    });
    return response.data;
};

export const updatePushToken = async (userId, pushToken) => {
    const response = await apiClient.put('/Volunteer/push-token', {
        userId,
        pushToken,
    });
    return response.data;
};

// Buyer API
export const getAllDatasets = async () => {
    const response = await apiClient.get('/Dataset');
    return response.data;
};

export const createCheckoutSession = async (datasetId) => {
    const response = await apiClient.post('/Buyer/checkout', { datasetId });
    return response.data;
};

export const getDatasetPreview = async (datasetId) => {
    const response = await apiClient.get(`/Dataset/${datasetId}/preview`);
    return response.data;
};

// Admin API
export const getAllVolunteers = async () => {
    const response = await apiClient.get('/Volunteer');
    return response.data;
};

export const updateVolunteerStatus = async (volunteerId, status) => {
    const response = await apiClient.put(`/Volunteer/${volunteerId}/status`, { status });
    return response.data;
};

export const getAdminStats = async () => {
    const response = await apiClient.get('/Admin/stats');
    return response.data;
};

export const createDataset = async (datasetData) => {
    const response = await apiClient.post('/Dataset', datasetData);
    return response.data;
};

export default apiClient;
