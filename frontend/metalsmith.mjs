import { fileURLToPath } from 'url';
import * as path from 'path';
import Metalsmith from 'metalsmith';
import layouts from '@metalsmith/layouts';
import discoverPartials from 'metalsmith-discover-partials';
import when from 'metalsmith-if';
import htmlMinifier from 'metalsmith-html-minifier';
import sitemap from 'metalsmith-sitemap';

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

function getNamespacesFile(files, ext) {
    return files[`namespaces${ext}`];
}

function getNamespaceFiles(files, ext) {
    const namespaces = getNamespacesFile(files, ext)['data'];
    return _.map(namespaces, (urlSlug, catalogNamespace) => {
        return files[`namespaces/${catalogNamespace}/index${ext}`];
    });
}

function getItemFiles(catalogNamespace, files, ext) {
    const pathPrefix = `namespaces/${catalogNamespace}/`;
    const filteredFilepaths = _.filter(_.keys(files), filepath => filepath.startsWith(pathPrefix));
    return _.map(filteredFilepaths, filepath => files[filepath]);
}

function getAllItemFiles(files, ext) {
    const index = `index${ext}`;
    const filteredFilepaths = _.filter(_.keys(files), filepath => {
        return filepath.startsWith('namespaces/') && !filepath.endsWith(index);
    });
    return _.map(filteredFilepaths, filepath => files[filepath]);
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
    const namespacesFile = getNamespacesFile(files, '.json');
    const namespaces = namespacesFile['data'];

    _.forEach(namespaces, (urlSlug, catalogNamespace) => {
        const pathPrefix = `namespaces/${catalogNamespace}/`;
        const items = _.map(getItemFiles(catalogNamespace, files, '.json'), file => file['data']);

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
            layout: 'namespace.hbs',
        };
    });
}

// Sets the 'file.layout' value based on the path
function setLayout(files, metalsmith) {
    files['namespaces.json']['layout'] = 'namespaces.hbs';

    const itemFiles = getAllItemFiles(files, '.json');
    _.forEach(itemFiles, file => file['layout'] = 'item.hbs');
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

// Adds page specific metadata
function addMetadata(files, metalsmith) {
    metalsmith.match('**/*.html')
        .forEach(filepath => {
            const file = files[filepath];
            file['metadata'] = {
                canonicalURL: `${metalsmith._metadata.siteData.baseURL}/${filepath}`
            };
        });
}

// Adds a 'robots.txt' file to the build
function addRobots(files, metalsmith) {
    const file = {
        contents: Buffer.from(`User-agent: *\nAllow:/\nSitemap: ${metalsmith._metadata.siteData.baseURL}/sitemap.xml`),
        mode: '0644'
    };

    files['robots.txt'] = file;
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

function getHost() {
    if (!process.env.BASE_URL) {
        return path.join(__dirname, 'build');
    }

    const url = new URL(process.env.BASE_URL);
    return `${url.protocol}//${url.host}`;
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
                    host: getHost(),
                    baseURL: getBaseURL(),
                    baseTitle: "Unofficial Epic Games Store Database",
                    generator: "Metalsmith + Handlebars"
                }
            })
            .use(removeNonJSON)
            .use(parseJSON)
            .use(mapData)
            .use(setLayout)
            .use(renameFiles)
            .use(changeExtensionToHTML)
            .use(addMetadata)
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
            .use(sitemap({
                hostname: getHost()
            }))
            .use(addRobots)
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
