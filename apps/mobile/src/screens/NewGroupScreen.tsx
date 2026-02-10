import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from 'react-native-paper';

export default function NewGroupScreen() {
    return (
        <View style={styles.container}>
            <Text variant="titleMedium">Create Group</Text>
            <Text variant="bodyMedium" style={styles.subtitle}>
                Add a name, avatar, and invite members
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
