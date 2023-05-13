declare module 'metalsmith-html-minifier' {
    import Metalsmith from 'metalsmith';

    declare function minifier(config: Object) : Metalsmith.PLugin;

    export = minifier;
}
