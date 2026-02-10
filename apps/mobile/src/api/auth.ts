import apiClient from './client';

// ── Types matching the .NET API models ──────────────────────────────

export interface UserInfo {
    id: string;
    userName: string;
    displayName: string;
    avatarUrl: string | null;
}

export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: UserInfo;
}

// ── API calls ───────────────────────────────────────────────────────

export async function login(userName: string, password: string): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/login', {
        userName,
        password,
    });
    return data;
}

export async function register(
    userName: string,
    email: string,
    password: string,
    displayName: string,
): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/register', {
        userName,
        email,
        password,
        displayName,
    });
    return data;
}

export async function refresh(refreshToken: string): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/refresh', {
        refreshToken,
    });
    return data;
}
