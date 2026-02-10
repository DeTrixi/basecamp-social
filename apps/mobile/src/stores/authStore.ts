import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import * as authApi from '../api/auth';
import type { UserInfo } from '../api/auth';

// ── Types ───────────────────────────────────────────────────────────

interface AuthState {
    isAuthenticated: boolean;
    user: UserInfo | null;
    isLoading: boolean; // true while restoring session on app launch

    login: (userName: string, password: string) => Promise<void>;
    register: (userName: string, email: string, password: string, displayName: string) => Promise<void>;
    logout: () => Promise<void>;
    restoreSession: () => Promise<void>;
}

// ── Store ───────────────────────────────────────────────────────────

export const useAuthStore = create<AuthState>((set) => ({
    isAuthenticated: false,
    user: null,
    isLoading: true,

    login: async (userName, password) => {
        const res = await authApi.login(userName, password);
        await persistTokens(res.accessToken, res.refreshToken);
        set({ isAuthenticated: true, user: res.user });
    },

    register: async (userName, email, password, displayName) => {
        const res = await authApi.register(userName, email, password, displayName);
        await persistTokens(res.accessToken, res.refreshToken);
        set({ isAuthenticated: true, user: res.user });
    },

    logout: async () => {
        await SecureStore.deleteItemAsync('accessToken');
        await SecureStore.deleteItemAsync('refreshToken');
        set({ isAuthenticated: false, user: null });
    },

    restoreSession: async () => {
        try {
            const refreshToken = await SecureStore.getItemAsync('refreshToken');
            if (!refreshToken) {
                set({ isLoading: false });
                return;
            }

            const res = await authApi.refresh(refreshToken);
            await persistTokens(res.accessToken, res.refreshToken);
            set({ isAuthenticated: true, user: res.user, isLoading: false });
        } catch {
            // Token expired or server unreachable — stay logged out
            await SecureStore.deleteItemAsync('accessToken');
            await SecureStore.deleteItemAsync('refreshToken');
            set({ isAuthenticated: false, user: null, isLoading: false });
        }
    },
}));

// ── Helpers ─────────────────────────────────────────────────────────

async function persistTokens(accessToken: string, refreshToken: string) {
    await SecureStore.setItemAsync('accessToken', accessToken);
    await SecureStore.setItemAsync('refreshToken', refreshToken);
}
