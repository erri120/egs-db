import { fileURLToPath } from 'url';
import * as path from 'path';
import Metalsmith from 'metalsmith';
import layouts from '@metalsmith/layouts';
import discoverPartials from 'metalsmith-discover-partials';
import when from 'metalsmith-if';
import htmlMinifier from 'metalsmith-html-minifier';

import * as _ from 'lodash-es';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const isProduction = process.env.NODE_ENV === 'production';
const isDevelopment = process.env.NODE_ENV === 'development';

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
    const namespacesFile = files['namespaces.json'];
    const namespaces = namespacesFile['data'];
    namespacesFile['seoData'] = {
        'title': 'Epic Games Store Database | Namespaces',
        'description': 'Unofficial database for the Epic Games Store.'
    }

    _.forEach(namespaces, (urlSlug, catalogNamespace) => {
        const pathPrefix = `namespaces/${catalogNamespace}/`;

        const filteredFilepaths = _.filter(_.keys(files), filepath => filepath.startsWith(pathPrefix));
        const items = _.map(filteredFilepaths, filepath => {
            const file = files[filepath];
            const fileData = file['data'];
            file['seoData'] = {
                'title': `Epic Games Store Database | ${fileData['title']}`,
                'description': fileData['description'],
            };
            return fileData;
        });

        const filePath = pathPrefix+'index.json';
        const data = {
            id: catalogNamespace,
            urlSlug,
            items,
        }

        files[filePath] = {
            contents: Buffer.from('tmp'),
            mode: '0664',
            data,
            seoData: {
                'title': `Epic Games Store Database | ${catalogNamespace}`,
                'description': `List of all catalog items in the namespace "${catalogNamespace}" (${urlSlug}).`
            },
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
            if ('layout' in file) return;
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

// Adds the static files from the 'public' folder to the build
function addStaticFiles(files, metalsmith, done) {
    const publicPath = path.join(__dirname, 'public');
    metalsmith.read(publicPath, (err, staticFiles) => {
        if (err) return done(err);
        Object.keys(staticFiles).forEach(filepath => files[filepath] = staticFiles[filepath]);
        return done();
    });
}

function getBaseURL() {
    if (process.env.BASE_URL) {
        return process.env.BASE_URL;
    }

    return path.join(__dirname, 'build');
}

export default async function build() {
    try {
        const t1 = performance.now();

        const ms = Metalsmith(__dirname);
        await ms
            .source('../data-dump')
            .destination('./build')
            .clean(isProduction)
            .env('NODE_ENV', process.env.NODE_ENV)
            .env('DEBUG', process.env.DEBUG)
            .metadata({
                siteData: {
                    isDevelopment,
                    isProduction,
                    baseURL: getBaseURL(),
                    baseTitle: "Epic Games Store Database",
                    generator: "Metalsmith + Handlebars"
                }
            })
            .use(removeNonJSON)
            .use(parseJSON)
            .use(mapData)
            .use(setLayout)
            .use(renameFiles)
            .use(changeExtensionToHTML)
            .use(discoverPartials({
                directory: 'layouts/partials',
                pattern: /\.(hbs|html)$/,
            }))
            .use(layouts({
                default: false,
                directory: 'layouts',
                suppressNoFilesError: false,
                engineOptions: {
                    strict: true,
                },
            }))
            .use(addStaticFiles)
            .use(when(isProduction, htmlMinifier({
                pattern: '**/*.html',
                minifierOptions: {
                    collapseBooleanAttributes: true,
                    collapseWhitespace: true,
                    removeAttributeQuotes: true,
                    removeComments: true,
                    removeEmptyAttributes: true,
                    removeRedundantAttributes: true,
                }
            })))
            .build();

        const t2 = performance.now();
        const duration = (t2 - t1) / 1000;
        console.log(`Build successful after ${duration.toFixed(2)} seconds`);
    } catch (err) {
        console.log('Build failed!');
        console.error(err);
    }
}

const isMainScript = process.argv[1] === fileURLToPath(import.meta.url);

if (isMainScript) {
    build();
}
