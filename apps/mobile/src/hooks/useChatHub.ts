import { useEffect, useRef } from 'react';
import { AppState } from 'react-native';
import { useAuthStore } from '../stores/authStore';
import { useChatHubStore } from '../stores/chatHubStore';
import { setOnline } from '../api/chatHub';

export function useChatHub() {
    const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
    const connect = useChatHubStore((s) => s.connect);
    const disconnect = useChatHubStore((s) => s.disconnect);
    const status = useChatHubStore((s) => s.status);

    const intentionalDisconnect = useRef(false);

    // Connect when authenticated, disconnect on logout
    useEffect(() => {
        if (isAuthenticated) {
            intentionalDisconnect.current = false;
            connect();
        } else {
            intentionalDisconnect.current = true;
            disconnect();
        }
    }, [isAuthenticated, connect, disconnect]);

    // Handle app foreground/background transitions
    useEffect(() => {
        const subscription = AppState.addEventListener('change', (nextState) => {
            if (nextState !== 'active') return;
            if (intentionalDisconnect.current) return;

            const currentStatus = useChatHubStore.getState().status;

            if (currentStatus === 'disconnected') {
                useChatHubStore.getState().connect();
            } else if (currentStatus === 'connected') {
                setOnline().catch(() => {});
            }
        });

        return () => subscription.remove();
    }, []);

    return status;
}
