import {
    HubConnectionBuilder,
    HubConnection,
    HttpTransportType,
    LogLevel,
} from '@microsoft/signalr';
import * as SecureStore from 'expo-secure-store';
import { BASE_URL } from './client';

// ── Server → Client event payloads ─────────────────────────────────

export interface ReceiveMessagePayload {
    conversationId: string;
    encryptedPayload: string; // base64-encoded byte[]
    senderId: string;
    timestamp: string;
    serverMessageId: string;
    clientMessageId: string;
}

export interface MessageDeliveredPayload {
    conversationId: string;
    messageId: string;
    clientMessageId: string;
}

export interface MessageReadPayload {
    conversationId: string;
    messageId: string;
    readBy: string;
}

export interface TypingUpdatePayload {
    conversationId: string;
    userId: string;
    isTyping: boolean;
}

export interface PresenceChangedPayload {
    userId: string;
    status: 'Online' | 'Offline';
    lastSeen: string;
}

// ── Event map for type-safe subscriptions ───────────────────────────

export interface ChatHubEvents {
    ReceiveMessage: (payload: ReceiveMessagePayload) => void;
    MessageDelivered: (payload: MessageDeliveredPayload) => void;
    MessageRead: (payload: MessageReadPayload) => void;
    TypingUpdate: (payload: TypingUpdatePayload) => void;
    PresenceChanged: (payload: PresenceChangedPayload) => void;
    Error: (message: string) => void;
}

export type ChatHubEventName = keyof ChatHubEvents;

// ── Connection singleton ────────────────────────────────────────────

let connection: HubConnection | null = null;

export function getConnection(): HubConnection | null {
    return connection;
}

export function createConnection(): HubConnection {
    if (connection) return connection;

    connection = new HubConnectionBuilder()
        .withUrl(`${BASE_URL}/hubs/chat`, {
            transport: HttpTransportType.WebSockets,
            skipNegotiation: true,
            accessTokenFactory: async () => {
                const token = await SecureStore.getItemAsync('accessToken');
                return token ?? '';
            },
        })
        .withAutomaticReconnect([0, 1_000, 5_000, 10_000, 30_000])
        .configureLogging(LogLevel.Warning)
        .build();

    return connection;
}

export async function startConnection(): Promise<void> {
    const conn = connection ?? createConnection();
    await conn.start();
}

export async function stopConnection(): Promise<void> {
    if (!connection) return;
    await connection.stop();
    connection = null;
}

// ── Invoke wrappers ─────────────────────────────────────────────────

export function sendMessage(
    conversationId: string,
    encryptedPayload: string,
    clientMessageId: string,
    messageType: string = 'Text',
): Promise<void> {
    if (!connection) throw new Error('Hub not connected');
    return connection.invoke(
        'SendMessage',
        conversationId,
        encryptedPayload,
        clientMessageId,
        messageType,
    );
}

export function markMessageRead(
    conversationId: string,
    messageId: string,
): Promise<void> {
    if (!connection) throw new Error('Hub not connected');
    return connection.invoke('MessageRead', conversationId, messageId);
}

export function startTyping(conversationId: string): Promise<void> {
    if (!connection) throw new Error('Hub not connected');
    return connection.send('StartTyping', conversationId);
}

export function stopTyping(conversationId: string): Promise<void> {
    if (!connection) throw new Error('Hub not connected');
    return connection.send('StopTyping', conversationId);
}

export function setOnline(): Promise<void> {
    if (!connection) throw new Error('Hub not connected');
    return connection.send('SetOnline');
}
