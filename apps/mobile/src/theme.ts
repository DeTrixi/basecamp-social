import { DefaultTheme } from 'react-native-paper';

export const theme = {
    ...DefaultTheme,
    colors: {
        ...DefaultTheme.colors,
        primary: '#2563EB',
        primaryContainer: '#DBEAFE',
        secondary: '#7C3AED',
        secondaryContainer: '#EDE9FE',
        background: '#FFFFFF',
        surface: '#FFFFFF',
        surfaceVariant: '#F1F5F9',
        error: '#DC2626',
        onPrimary: '#FFFFFF',
        onSecondary: '#FFFFFF',
        onBackground: '#0F172A',
        onSurface: '#0F172A',
        onSurfaceVariant: '#64748B',
        outline: '#CBD5E1',
    },
    roundness: 12,
};

export type AppTheme = typeof theme;
