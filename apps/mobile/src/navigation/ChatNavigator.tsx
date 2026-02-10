import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { ChatStackParamList } from './types';

import ConversationListScreen from '../screens/ConversationListScreen';
import ChatScreen from '../screens/ChatScreen';
import ConversationInfoScreen from '../screens/ConversationInfoScreen';
import NewConversationScreen from '../screens/NewConversationScreen';
import NewGroupScreen from '../screens/NewGroupScreen';

const Stack = createNativeStackNavigator<ChatStackParamList>();

export default function ChatNavigator() {
    return (
        <Stack.Navigator
            screenOptions={{
                headerShadowVisible: false,
                headerStyle: { backgroundColor: '#FFFFFF' },
                headerTintColor: '#2563EB',
            }}
        >
            <Stack.Screen
                name="ConversationList"
                component={ConversationListScreen}
                options={{ title: 'Chats' }}
            />
            <Stack.Screen
                name="Chat"
                component={ChatScreen}
                options={({ route }) => ({ title: route.params.title })}
            />
            <Stack.Screen
                name="ConversationInfo"
                component={ConversationInfoScreen}
                options={{ title: 'Info' }}
            />
            <Stack.Screen
                name="NewConversation"
                component={NewConversationScreen}
                options={{ title: 'New Chat', presentation: 'modal' }}
            />
            <Stack.Screen
                name="NewGroup"
                component={NewGroupScreen}
                options={{ title: 'New Group', presentation: 'modal' }}
            />
        </Stack.Navigator>
    );
}
