import axios from 'axios';
import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';

// iOS simulator can use localhost; Android emulator needs 10.0.2.2
const BASE_URL = Platform.select({
    android: 'http://10.0.2.2:5297',
    default: 'http://localhost:5297',
});

const apiClient = axios.create({
    baseURL: `${BASE_URL}/api/v1`,
    headers: { 'Content-Type': 'application/json' },
    timeout: 15_000,
});

// Attach access token to every request
apiClient.interceptors.request.use(async (config) => {
    const token = await SecureStore.getItemAsync('accessToken');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// On 401, try to refresh; if that fails, clear tokens
apiClient.interceptors.response.use(
    (response) => response,
    async (error) => {
        const original = error.config;

        if (error.response?.status === 401 && !original._retry) {
            original._retry = true;

            const refreshToken = await SecureStore.getItemAsync('refreshToken');
            if (!refreshToken) return Promise.reject(error);

            try {
                const { data } = await axios.post(
                    `${BASE_URL}/api/v1/auth/refresh`,
                    { refreshToken },
                    { headers: { 'Content-Type': 'application/json' } },
                );

                await SecureStore.setItemAsync('accessToken', data.accessToken);
                await SecureStore.setItemAsync('refreshToken', data.refreshToken);

                original.headers.Authorization = `Bearer ${data.accessToken}`;
                return apiClient(original);
            } catch {
                // Refresh failed — tokens are stale
                await SecureStore.deleteItemAsync('accessToken');
                await SecureStore.deleteItemAsync('refreshToken');
                return Promise.reject(error);
            }
        }

        return Promise.reject(error);
    },
);

export default apiClient;
