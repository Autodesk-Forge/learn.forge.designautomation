const _path = require('path');
const express = require('express');
const cookieSession = require('cookie-session');
const config = require('./config');
if (!config.credentials.client_id || !config.credentials.client_secret)
    return (console.error('Missing FORGE_CLIENT_ID or FORGE_CLIENT_SECRET env variables.'));

let app = express();
app.use(express.static(_path.join(__dirname, './public')));
app.use(cookieSession({
    name: 'forge_session',
    keys: ['forge_secure_key'],
    maxAge: 60 * 60 * 1000 // 1 hour, same as the 2 legged lifespan token
}));
app.use(express.json({
    limit: '50mb'
}));
app.use('/api', require('./routes/DesignAutomation'));

app.set('port', process.env.PORT || 3000);

module.exports = app;
