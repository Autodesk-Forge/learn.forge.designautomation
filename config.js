// Autodesk Forge configuration
module.exports = {
    // Set environment variables or hard-code here
    credentials: {
        client_id: process.env.FORGE_CLIENT_ID,
        client_secret: process.env.FORGE_CLIENT_SECRET,
        callback_url: process.env.FORGE_CALLBACK || process.env.FORGE_CALLBACK_URL,
        webhook_url: process.env.FORGE_WEBHOOK_URL
    },
    scopes: {
        // Required scopes for the server-side application
        internal: ['bucket:create', 'bucket:read', 'bucket:delete', 'data:read', 'data:create', 'data:write', 'code:all'],
        // Required scope for the client-side viewer
        public: ['viewables:read']
    },
    client: {
        circuitBreaker: {
            threshold: 11,
            interval: 1200
        },
        retry: {
            maxNumberOfRetries: 7,
            backoffDelay: 4000,
            backoffPolicy: 'exponentialBackoffWithJitter'
        },
        requestTimeout: 13000
    }
};
