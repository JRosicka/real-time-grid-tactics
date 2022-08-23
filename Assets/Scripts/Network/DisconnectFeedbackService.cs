public static class DisconnectFeedbackService {
    public enum DisconnectReason {
        NotDisconnected,   // Special case for before having left a room at all this launch. We have not disconnected from a room yet. 
        Unknown,           // Default reason that we default to if we don't know what the disconnect reason was. Probably the server disconnecting or some issue with client's connection to server. 
        ClientDisconnect,  // This client voluntarily disconnected from the server.
        Kicked             // The server kicked the client.
    }

    private static DisconnectReason _lastDisconnectReason = DisconnectReason.NotDisconnected;
    private static bool _disconnected;
    public static void SetDisconnectReason(DisconnectReason reason) {
        _lastDisconnectReason = reason;
    }

    public static void SetDisconnected() {
        _disconnected = true;
    }

    public static DisconnectReason ProcessLastDisconnect() {
        if (!_disconnected) {
            return DisconnectReason.NotDisconnected;
        }

        DisconnectReason ret = _lastDisconnectReason;
        
        // Reset to default state
        _lastDisconnectReason = DisconnectReason.Unknown;
        _disconnected = false;
        
        return ret;
    }
}
