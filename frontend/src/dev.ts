// noinspection HttpUrlsUsage

import browserSync from 'browser-sync';
import chokidar from 'chokidar';
import build from './build';
import path from "path";

const cwd = path.dirname(__dirname);
const defaultHost = 'localhost';
const defaultPort = 8000;
const baseURL = `http://${defaultHost}:${defaultPort}`;

process.env.BASE_URL = baseURL;

chokidar
    .watch([path.join(cwd, 'layouts'), path.join(cwd, 'public')], {
        ignoreInitial: true,
    })
    .on('ready', () => browserSync.init({
        host: defaultHost,
        port: defaultPort,
        server: path.join(cwd, 'build'),
        injectChanges: true,
        open: false,
    }))
    .on('all', async () => {
        try {
            await build();
            browserSync.reload();
        } catch (err) {
            console.error(err);
        }
    });

(async function() {
    await build();
}());
