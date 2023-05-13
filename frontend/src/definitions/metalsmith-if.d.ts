declare module 'metalsmith-if' {
    import Metalsmith from 'metalsmith';

    declare function when(condition: boolean, plugin: Metalsmith.Plugin) : Metalsmith.Plugin;

    export = when;
}
