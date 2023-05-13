import browserSync from 'browser-sync';
import chokidar from 'chokidar';
import build from './metalsmith.mjs';

process.env.BASE_URL = 'http://localhost:8000'

chokidar
    .watch(['layouts', 'public'], {
        ignoreInitial: true,
    })
    .on('ready', () => browserSync.init({
        host: 'localhost',
        port: 8000,
        server: './build',
        injectChanges: true, // false = prefer full reload
        interval: 20000,
        open: false,
    }))
    .on('all', async (...args) => {
        console.log('Building...');
        try {
            await build();
            browserSync.reload();
        } catch (err) {
            console.error(err);
        }
    });

(async function() {
    await build();
}())
