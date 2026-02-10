export type AuthStackParamList = {
    Login: undefined;
    Register: undefined;
};

export type MainTabParamList = {
    Chats: undefined;
    Contacts: undefined;
    Settings: undefined;
};

export type ChatStackParamList = {
    ConversationList: undefined;
    Chat: { conversationId: string; title: string };
    ConversationInfo: { conversationId: string };
    NewConversation: undefined;
    NewGroup: undefined;
};

export type RootStackParamList = {
    Auth: undefined;
    Main: undefined;
};
