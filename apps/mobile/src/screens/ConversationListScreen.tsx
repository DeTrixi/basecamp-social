import React from 'react';
import { View, StyleSheet, FlatList } from 'react-native';
import { Text, FAB } from 'react-native-paper';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { ChatStackParamList } from '../navigation/types';

type Props = NativeStackScreenProps<ChatStackParamList, 'ConversationList'>;

export default function ConversationListScreen({ navigation }: Props) {
    return (
        <View style={styles.container}>
            <View style={styles.empty}>
                <Text variant="titleMedium" style={styles.emptyText}>
                    No conversations yet
                </Text>
                <Text variant="bodyMedium" style={styles.emptySubtext}>
                    Start a new chat to begin messaging
                </Text>
            </View>

            <FAB
                icon="plus"
                style={styles.fab}
                onPress={() => navigation.navigate('NewConversation')}
            />
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#FFFFFF',
    },
    empty: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: 24,
    },
    emptyText: {
        color: '#0F172A',
        marginBottom: 4,
    },
    emptySubtext: {
        color: '#64748B',
    },
    fab: {
        position: 'absolute',
        right: 16,
        bottom: 16,
    },
});
