const app = require('./server');
const socketIO = require('./socket.io')(app);

let server = socketIO.http.listen(app.get('port'), () => {
    console.log(`Server listening on port ${app.get('port')}`);
});

server.on('error', (err) => {
    if (err.errno === 'EACCES') {
        console.error(`Port ${app.get('port')} already in use.\nExiting...`);
        process.exit (1) ;
    }
}) ;
