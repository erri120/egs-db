import * as path from 'path';

import Metalsmith from 'metalsmith';
import when from 'metalsmith-if';
import htmlMinifier from 'metalsmith-html-minifier';
import sitemap from "metalsmith-sitemap";
import discoverPartials from "metalsmith-discover-partials";
import layouts from "@metalsmith/layouts";

const cwd = path.dirname(__dirname);
const isProduction = process.env.NODE_ENV === 'production';
const isDevelopment = process.env.NODE_ENV === 'development';

type CatalogNamespace = string;
type UrlSlug = string;

interface Namespaces {
    [catalogNamespace: CatalogNamespace]: UrlSlug
}

function getBaseURL(): string {
    return process.env.BASE_URL ?? `file:///${path.join(cwd, 'build')}`;
}

function getHost(): string {
    if (!process.env.BASE_URL) return 'https://example.org';
    const url = new URL(process.env.BASE_URL);
    return `${url.protocol}//${url.host}`;
}

// Returns all namespaces.
function getNamespaces(files: Metalsmith.Files): Namespaces {
    const namespacesFile = files['namespaces.json'];
    return namespacesFile['data'];
}

// Returns all item files for a given namespace.
function getNamespaceItemFiles(catalogNamespace: CatalogNamespace, files: Metalsmith.Files): Metalsmith.Files[] {
    const pathPrefix = `namespaces/${catalogNamespace}/`;
    return Object.keys(files)
        .filter(filepath => filepath.startsWith(pathPrefix))
        .map(filepath => files[filepath]);
}

// Removes all non-JSON files from the build.
function removeNonJSON(files: Metalsmith.Files) {
    Object.keys(files)
        .filter(filepath => !filepath.endsWith('.json'))
        .forEach(filepath => delete files[filepath]);
}

// Calls `JSON.parse` on every file and puts the result into `file['data']`.
function parseJSON(files: Metalsmith.Files) {
    for (const filepath in files) {
        const file = files[filepath];
        // @ts-ignore
        file['data'] = JSON.parse(file.contents);
    }
}

// Creates index.json files for every namespace
function createNamespaceFiles(files: Metalsmith.Files) {
    const namespaces = getNamespaces(files);

    for (const catalogNamespace in namespaces) {
        const pathPrefix = `namespaces/${catalogNamespace}/`;
        const itemFiles = getNamespaceItemFiles(catalogNamespace, files);
        const items = itemFiles.map(file => file['data']);

        const namespaceFilePath = pathPrefix + 'index.json';
        const data = {
            id: catalogNamespace,
            urlSlug: namespaces[catalogNamespace],
            items,
        };

        files[namespaceFilePath] = {
            contents: Buffer.from(''),
            mods: '0644',
            data,
            layout: 'namespace.hbs',
        };
    }
}

// Fixes up the `namespaces.json` file.
function fixNamespacesFile(files: Metalsmith.Files) {
    const file = files['namespaces.json'];
    file['layout'] = 'namespaces.hbs';
    files['index.json'] = file;
    delete files['namespaces.json'];
}

// Changes the extension of every file to `.html`.
function changeExtensionToHTML(files: Metalsmith.Files) {
    Object
        .keys(files)
        .forEach(filepath => {
            const newFilePath = path.join(path.dirname(filepath), path.basename(filepath, '.json') + '.html');
            files[newFilePath] = files[filepath];
            delete files[filepath];
        });
}

// Adds page specific metadata like the canonical URL.
function addPageMetadata(files: Metalsmith.Files, metalsmith: Metalsmith) {
    const globalMetadata: {[property: string]: any} = metalsmith.metadata();
    const siteData: {[property: string]: string} = globalMetadata['siteData'];

    Object
        .keys(files)
        .forEach(filepath => {
            const file = files[filepath];
            const data: {[property: string]: string} = file['data'];

            let metadata: {[property: string]: string} = {
                canonicalURL: `${getBaseURL()}/${filepath}`,
            }

            if (filepath === 'index.html') {
                metadata['title'] = siteData['baseTitle'];
                metadata['description'] = 'Unofficial database of the Epic Games Store.';
            } else if (filepath.endsWith('index.html')) {
                metadata['title'] = `${data['id']} | ${siteData['baseTitle']}`;
                metadata['description'] = `List of all items in the namespace ${data['id']} (${data['urlSlug']})`;
            } else {
                metadata['title'] = `${data['title']} | ${siteData['baseTitle']}`;
                metadata['description'] = `Information about item "${data['id']}" in the namespace "${data['namespace']}" by ${data['developer']}`;
            }

            file['metadata'] = metadata;
        });
}

// Creates a `robots.txt` file.
function createRobotsTxt(files: Metalsmith.Files) {
    const contents = `
User-Agent: *
Allow: /
Sitemap: ${getBaseURL()}/sitemap.xml
`;

    files['robots.txt'] = {
        contents: Buffer.from(contents),
        mode: '0644',
    };
}

// Adds the static files from the `public` folder to the build.
function addStaticFiles(files: Metalsmith.Files, metalsmith: Metalsmith, done: Metalsmith.DoneCallback) {
    const publicPath = path.join(cwd, 'public');
    metalsmith.read(publicPath, (err, staticFiles) => {
        if (err) {
            done(err);
            return;
        }

        Object
            .keys(staticFiles)
            .forEach(filepath => files[filepath] = staticFiles[filepath]);

        done();
    });
}

export default async function build() {
    console.log('Building...');
    try {
        const t1 = performance.now();

        const ms = Metalsmith(cwd);
        await ms
            .source(path.join(path.dirname(cwd), 'data-dump'))
            .destination(path.join(cwd, 'build'))
            .clean(isProduction)
            .env('NODE_ENV', process.env.NODE_ENV || '')
            .env('DEBUG', process.env.DEBUG || false)
            .metadata({
                siteData: {
                    isDevelopment,
                    isProduction,
                    host: getHost(),
                    baseURL: getBaseURL(),
                    baseTitle: 'Unofficial Epic Games Store Database',
                    generator: 'Metalsmith + Handlebars'
                }
            })
            .use(removeNonJSON)
            .use(parseJSON)
            .use(createNamespaceFiles)
            .use(fixNamespacesFile)
            .use(changeExtensionToHTML)
            .use(addPageMetadata)
            .use(discoverPartials({
                directory: 'layouts/partials',
                pattern: /\.(hbs|html)$/,
            }))
            .use(layouts({
                default: 'item.hbs',
                directory: 'layouts',
                suppressNoFilesError: false,
                engineOptions: {
                    strict: true,
                },
            }))
            .use(sitemap({
                hostname: getHost()
            }))
            .use(createRobotsTxt)
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

if (process.argv[1] == __filename) {
    (async function() {
        await build();
    }());
}
