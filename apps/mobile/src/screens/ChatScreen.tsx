import React, { useState } from 'react';
import { View, StyleSheet, FlatList, KeyboardAvoidingView, Platform } from 'react-native';
import { Text, TextInput, IconButton } from 'react-native-paper';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { ChatStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<ChatStackParamList, 'Chat'>;

export default function ChatScreen({ route }: Props) {
    const { conversationId, title } = route.params;
    const [message, setMessage] = useState('');

    const handleSend = () => {
        if (!message.trim()) return;
        // TODO: Send encrypted message via SignalR
        setMessage('');
    };

    return (
        <KeyboardAvoidingView
            style={styles.container}
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
            keyboardVerticalOffset={90}
        >
            <View style={styles.messages}>
                <View style={styles.empty}>
                    <Text variant="bodyMedium" style={styles.emptyText}>
                        No messages yet. Say hello!
                    </Text>
                </View>
            </View>

            <View style={styles.inputBar}>
                <TextInput
                    value={message}
                    onChangeText={setMessage}
                    placeholder="Message"
                    mode="outlined"
                    style={styles.input}
                    dense
                    right={
                        <TextInput.Icon
                            icon="send"
                            onPress={handleSend}
                            disabled={!message.trim()}
                        />
                    }
                />
            </View>
        </KeyboardAvoidingView>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#FFFFFF',
    },
    messages: {
        flex: 1,
    },
    empty: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: 24,
    },
    emptyText: {
        color: '#64748B',
    },
    inputBar: {
        padding: 8,
        borderTopWidth: 1,
        borderTopColor: '#E2E8F0',
        backgroundColor: '#FFFFFF',
    },
    input: {
        backgroundColor: '#F8FAFC',
    },
});
