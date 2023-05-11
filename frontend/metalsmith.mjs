import { fileURLToPath } from 'url';
import * as path from 'path';
import Metalsmith from 'metalsmith';
import layouts from '@metalsmith/layouts';
import discoverPartials from 'metalsmith-discover-partials';

function timeStep(step, stepName) {
    stepName = step.name;

    return function timeStep(files, metalsmith, done) {
        const t1 = performance.now();
        step(files, metalsmith);
        const t2 = performance.now();

        const duration = (t2 - t1) / 1000;
        console.log(`${stepName}: ${duration.toFixed(3)}s`);

        return done();
    }
}

// Removes all non-JSON files from the build
function removeNonJSON(files, metalsmith) {
    Object.keys(files)
        .filter(filepath => path.extname(filepath) != '.json')
        .forEach(filepath => delete files[filepath]);
}

// Parses all JSON files and puts the data into 'file.data'
function parseJSON(files, metalsmith) {
    metalsmith.match('**/*.json')
        .forEach(filepath => {
            const file = files[filepath];
            file['data'] = JSON.parse(file.contents);
        });
}

// Maps, combines and correlates existing data
function mapData(files, metalsmith) {
    const namespaces = files['namespaces.json']['data'];

    Object.entries(namespaces)
        .forEach(([catalogNamespace, urlSlug]) => {
            const items = metalsmith.match('namespaces/'+catalogNamespace+'/*.json')
                .map(filepath => {
                    const file = files[filepath];
                    const id = file['data']['id'];
                    const title = file['data']['title'];
                    const categories = file['data']['categories'];
                    return {
                        id,
                        title,
                        categories,
                    };
                });

            const filePath = 'namespaces/'+catalogNamespace+'/index.json';
            const data = {
                id: catalogNamespace,
                urlSlug: urlSlug,
                items,
            }

            files[filePath] = {
                contents: Buffer.from('tmp'),
                mode: '0664',
                data: data,
                layout: 'namespace.hbs',
            };
        });
}

// Sets the 'file.layout' value based on the path
function setLayout(files, metalsmith) {
    files['namespaces.json']['layout'] = 'namespaces.hbs';

    metalsmith.match('namespaces/**/*.json')
        .forEach(filepath => {
            const file = files[filepath];
            if (file['layout']) return;
            file['layout'] = 'item.hbs';
        });
}

// Renames files
function renameFiles(files, metalsmith) {
    files['index.json'] = files['namespaces.json']
    delete files['namespaces.json']
}

// Changes the file extension of JSON files to '.html'
function changeExtensionToHTML(files, metalsmith) {
    metalsmith.match('**/*.json')
        .forEach(filepath => {
            const newFilePath = path.join(path.dirname(filepath), path.basename(filepath, '.json') + '.html');
            files[newFilePath] = files[filepath];
            delete files[filepath];
        });
}

const __dirname = path.dirname(fileURLToPath(import.meta.url));

async function build() {
    try {
        const t1 = performance.now();

        const ms = Metalsmith(__dirname);
        const files = ms
            .source('../data-dump')
            .destination('./build')
            .clean(true)
            .env('NODE_ENV', process.env.NODE_ENV)
            .env('DEBUG', process.env.DEBUG)
            .metadata({
                siteData: {
                    baseURL: process.env.GITHUB_CI
                    ? 'https://erri120.github.io/egs-db'
                    : path.join(__dirname, 'build'),
                }
            })
            .use(timeStep(removeNonJSON))
            .use(timeStep(parseJSON))
            .use(timeStep(mapData))
            .use(timeStep(setLayout))
            .use(timeStep(renameFiles))
            .use(timeStep(changeExtensionToHTML))
            .use(discoverPartials({
                directory: 'layouts/partials',
                pattern: /\.(hbs|html)$/,
            }))
            .use(layouts({
                default: false,
                directory: 'layouts',
                suppressNoFilesError: false,
                engineOptions: {  },
            }))
            .build((err) => {
                if (err) throw err;
                const t2 = performance.now();
                const buildDuration = (t2 - t1) / 1000;
                console.log(`Build took ${buildDuration.toFixed(2)}s`);
            });

        return files;
    } catch (err) {
        console.error(err);
        return err;
    }
}

const isMainScript = process.argv[1] === fileURLToPath(import.meta.url);

if (isMainScript) {
    build();
} else {
    module.exports = build;
}
