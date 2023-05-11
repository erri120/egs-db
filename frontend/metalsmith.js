const path = require('path');
const Metalsmith = require('metalsmith');
const layouts = require('@metalsmith/layouts');
const collections = require('@metalsmith/collections');

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

const t1 = performance.now();

Metalsmith(__dirname)
    .source('../data-dump')
    .destination('./build')
    .clean(true)
    .env('NODE_ENV', process.env.NODE_ENV)
    .env('DEBUG', process.env.DEBUG)
    .use(removeNonJSON)
    .use(parseJSON)
    .use(mapData)
    .use(setLayout)
    .use(renameFiles)
    .use(changeExtensionToHTML)
    .use(layouts({
        default: false,
        directory: 'layouts',
        suppressNoFilesError: false,
        engineOptions: {  }
    }))
    .build((err) => {
        if (err) throw err;
        const t2 = performance.now();
        const duration = (t2 - t1) / 1000;
        console.log(`Build success in ${duration.toFixed(1)}s`);
    });

