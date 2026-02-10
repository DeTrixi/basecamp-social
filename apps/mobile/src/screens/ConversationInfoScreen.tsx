import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from 'react-native-paper';

export default function ConversationInfoScreen() {
    return (
        <View style={styles.container}>
            <Text variant="titleMedium">Conversation Info</Text>
            <Text variant="bodyMedium" style={styles.subtitle}>
                Members, settings, and shared media
            </Text>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#FFFFFF',
        justifyContent: 'center',
        alignItems: 'center',
        padding: 24,
    },
    subtitle: {
        color: '#64748B',
        marginTop: 4,
    },
});
