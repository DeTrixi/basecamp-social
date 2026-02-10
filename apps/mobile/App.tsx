import 'react-native-get-random-values';
import React, { useState } from 'react';
import { StatusBar } from 'expo-status-bar';
import { PaperProvider } from 'react-native-paper';
import RootNavigator from './src/navigation/RootNavigator';
import { theme } from './src/theme';

export default function App() {
  // TODO: Replace with Zustand auth store
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  return (
    <PaperProvider theme={theme}>
      <StatusBar style="auto" />
      <RootNavigator isAuthenticated={isAuthenticated} />
    </PaperProvider>
  );
}
