import React, { useState } from 'react';
import { View, StyleSheet, KeyboardAvoidingView, Platform } from 'react-native';
import { Text, TextInput, Button, HelperText } from 'react-native-paper';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AuthStackParamList } from '../navigation/types';
import { useAuthStore } from '../stores/authStore';
import axios from 'axios';

type Props = NativeStackScreenProps<AuthStackParamList, 'Login'>;

export default function LoginScreen({ navigation }: Props) {
    const [userName, setUserName] = useState('');
    const [password, setPassword] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const login = useAuthStore((s) => s.login);

    const handleLogin = async () => {
        setLoading(true);
        setError('');
        try {
            await login(userName, password);
        } catch (err) {
            if (axios.isAxiosError(err)) {
                setError(err.response?.data?.detail ?? err.response?.data?.title ?? 'Login failed');
            } else {
                setError('An unexpected error occurred');
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <KeyboardAvoidingView
            style={styles.container}
            behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        >
            <View style={styles.content}>
                <Text variant="headlineLarge" style={styles.title}>
                    Basecamp Social
                </Text>
                <Text variant="bodyLarge" style={styles.subtitle}>
                    Sign in to continue
                </Text>

                <TextInput
                    label="Username"
                    value={userName}
                    onChangeText={setUserName}
                    mode="outlined"
                    autoCapitalize="none"
                    style={styles.input}
                />

                <TextInput
                    label="Password"
                    value={password}
                    onChangeText={setPassword}
                    mode="outlined"
                    secureTextEntry
                    style={styles.input}
                />

                {error ? <HelperText type="error">{error}</HelperText> : null}

                <Button
                    mode="contained"
                    onPress={handleLogin}
                    loading={loading}
                    disabled={loading || !userName || !password}
                    style={styles.button}
                >
                    Sign In
                </Button>

                <Button
                    mode="text"
                    onPress={() => navigation.navigate('Register')}
                    style={styles.link}
                >
                    Don't have an account? Sign Up
                </Button>
            </View>
        </KeyboardAvoidingView>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#FFFFFF',
    },
    content: {
        flex: 1,
        justifyContent: 'center',
        padding: 24,
    },
    title: {
        textAlign: 'center',
        fontWeight: 'bold',
        marginBottom: 4,
    },
    subtitle: {
        textAlign: 'center',
        color: '#64748B',
        marginBottom: 32,
    },
    input: {
        marginBottom: 12,
    },
    button: {
        marginTop: 8,
        paddingVertical: 4,
    },
    link: {
        marginTop: 12,
    },
});
