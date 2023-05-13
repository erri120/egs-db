declare module 'metalsmith-sitemap' {
    import Metalsmith from 'metalsmith';

    declare function sitemap(config: Object) : Metalsmith.Plugin;

    export = sitemap;
}
