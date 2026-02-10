import React, { useState } from 'react';
import { View, StyleSheet } from 'react-native';
import { Text, List, Switch, Divider } from 'react-native-paper';
import { useAuthStore } from '../stores/authStore';

export default function SettingsScreen() {
    const [notifications, setNotifications] = useState(true);
    const logout = useAuthStore((s) => s.logout);

    const handleLogout = () => {
        logout();
    };

    return (
        <View style={styles.container}>
            <List.Section>
                <List.Subheader>Account</List.Subheader>
                <List.Item
                    title="Profile"
                    description="Update your display name and avatar"
                    left={(props) => <List.Icon {...props} icon="account" />}
                    onPress={() => { }}
                />
                <Divider />
                <List.Item
                    title="Notifications"
                    left={(props) => <List.Icon {...props} icon="bell" />}
                    right={() => (
                        <Switch value={notifications} onValueChange={setNotifications} />
                    )}
                />
            </List.Section>

            <List.Section>
                <List.Subheader>Security</List.Subheader>
                <List.Item
                    title="Encryption Keys"
                    description="View your identity key fingerprint"
                    left={(props) => <List.Icon {...props} icon="key" />}
                    onPress={() => { }}
                />
            </List.Section>

            <List.Section>
                <List.Subheader>About</List.Subheader>
                <List.Item
                    title="Version"
                    description="1.0.0"
                    left={(props) => <List.Icon {...props} icon="information" />}
                />
                <Divider />
                <List.Item
                    title="Sign Out"
                    left={(props) => <List.Icon {...props} icon="logout" color="#DC2626" />}
                    titleStyle={{ color: '#DC2626' }}
                    onPress={handleLogout}
                />
            </List.Section>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#FFFFFF',
    },
});
