import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import {
    createConnection,
    startConnection,
    stopConnection,
    getConnection,
    setOnline,
    sendMessage,
    markMessageRead,
    startTyping,
    stopTyping,
    type ChatHubEvents,
    type ChatHubEventName,
} from '../api/chatHub';

// ── Types ───────────────────────────────────────────────────────────

export type HubStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

interface ChatHubState {
    status: HubStatus;
    error: string | null;
    connect: () => Promise<void>;
    disconnect: () => Promise<void>;
    sendMessage: typeof sendMessage;
    markMessageRead: typeof markMessageRead;
    startTyping: typeof startTyping;
    stopTyping: typeof stopTyping;
}

// ── Listener registry (outside Zustand to avoid re-renders) ─────────

type Listener<E extends ChatHubEventName = ChatHubEventName> = ChatHubEvents[E];

const listeners = new Map<ChatHubEventName, Set<Listener<any>>>();

function getListenerSet<E extends ChatHubEventName>(event: E): Set<Listener<E>> {
    let set = listeners.get(event);
    if (!set) {
        set = new Set();
        listeners.set(event, set);
    }
    return set as Set<Listener<E>>;
}

/** Subscribe to a hub event. Returns an unsubscribe function. */
export function on<E extends ChatHubEventName>(
    event: E,
    handler: ChatHubEvents[E],
): () => void {
    const set = getListenerSet(event);
    set.add(handler);
    return () => {
        set.delete(handler);
    };
}

function dispatch<E extends ChatHubEventName>(
    event: E,
    ...args: Parameters<ChatHubEvents[E]>
) {
    const set = listeners.get(event);
    if (!set) return;
    for (const handler of set) {
        (handler as (...a: unknown[]) => void)(...args);
    }
}

// ── Event names to wire up ──────────────────────────────────────────

const EVENT_NAMES: ChatHubEventName[] = [
    'ReceiveMessage',
    'MessageDelivered',
    'MessageRead',
    'TypingUpdate',
    'PresenceChanged',
    'Error',
];

// ── Store ───────────────────────────────────────────────────────────

export const useChatHubStore = create<ChatHubState>((set, get) => ({
    status: 'disconnected',
    error: null,

    connect: async () => {
        if (get().status === 'connected' || get().status === 'connecting') return;

        set({ status: 'connecting', error: null });

        const conn = createConnection();

        // Wire server→client events to the listener registry
        for (const event of EVENT_NAMES) {
            conn.on(event, (...args: unknown[]) => {
                dispatch(event, ...(args as [any]));
            });
        }

        // Lifecycle callbacks
        conn.onreconnecting(() => {
            set({ status: 'reconnecting' });
        });

        conn.onreconnected(() => {
            set({ status: 'connected', error: null });
            setOnline().catch(() => {});
        });

        conn.onclose((err) => {
            set({ status: 'disconnected', error: err?.message ?? null });

            // If unexpected close (error present), attempt token refresh + reconnect once
            if (err) {
                tryReconnectOnce().catch(() => {});
            }
        });

        try {
            await startConnection();
            set({ status: 'connected' });
            setOnline().catch(() => {});
        } catch (err: any) {
            set({ status: 'disconnected', error: err?.message ?? 'Connection failed' });
        }
    },

    disconnect: async () => {
        await stopConnection();
        set({ status: 'disconnected', error: null });
    },

    sendMessage,
    markMessageRead,
    startTyping,
    stopTyping,
}));

// ── Helpers ─────────────────────────────────────────────────────────

async function tryReconnectOnce(): Promise<void> {
    const refreshToken = await SecureStore.getItemAsync('refreshToken');
    if (!refreshToken) return;

    try {
        // Use the same refresh endpoint as the API client
        const { default: axios } = await import('axios');
        const { BASE_URL } = await import('../api/client');

        const { data } = await axios.post(
            `${BASE_URL}/api/v1/auth/refresh`,
            { refreshToken },
            { headers: { 'Content-Type': 'application/json' } },
        );

        await SecureStore.setItemAsync('accessToken', data.accessToken);
        await SecureStore.setItemAsync('refreshToken', data.refreshToken);

        await useChatHubStore.getState().connect();
    } catch {
        // Refresh failed — stay disconnected
    }
}
