declare module 'metalsmith-discover-partials' {
    import Metalsmith from 'metalsmith';

    declare function discoverPartials(config: Object) : Metalsmith.Plugin;

    export = discoverPartials;
}
