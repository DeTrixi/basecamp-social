import 'react-native-get-random-values';
import React, { useEffect } from 'react';
import { StatusBar } from 'expo-status-bar';
import { ActivityIndicator, PaperProvider } from 'react-native-paper';
import { View } from 'react-native';
import RootNavigator from './src/navigation/RootNavigator';
import { theme } from './src/theme';
import { useAuthStore } from './src/stores/authStore';
import { useChatHub } from './src/hooks/useChatHub';

export default function App() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const isLoading = useAuthStore((s) => s.isLoading);
  const restoreSession = useAuthStore((s) => s.restoreSession);

  useEffect(() => {
    restoreSession();
  }, [restoreSession]);

  useChatHub();

  if (isLoading) {
    return (
      <PaperProvider theme={theme}>
        <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
          <ActivityIndicator size="large" />
        </View>
      </PaperProvider>
    );
  }

  return (
    <PaperProvider theme={theme}>
      <StatusBar style="auto" />
      <RootNavigator isAuthenticated={isAuthenticated} />
    </PaperProvider>
  );
}
