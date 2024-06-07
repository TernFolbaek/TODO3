import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export const createHubConnection = (token) => {
    return new HubConnectionBuilder()
        .withUrl('https://localhost:7060/todoHub', { accessTokenFactory: () => token })
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();
};
