import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text, Searchbar } from 'react-native-paper';
import { useState } from 'react';

export default function NewConversationScreen() {
    const [query, setQuery] = useState('');

    return (
        <View style={styles.container}>
            <Searchbar
                placeholder="Search users..."
                value={query}
                onChangeText={setQuery}
                style={styles.search}
            />
            <View style={styles.empty}>
                <Text variant="bodyMedium" style={styles.emptyText}>
                    Search for a user to start chatting
                </Text>
            </View>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#FFFFFF',
    },
    search: {
        margin: 12,
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
});
