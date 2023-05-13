declare module 'metalsmith' {

/// <reference types="node" />
    import {Mode, Stats} from 'fs';

    interface Metalsmith {
        directory(directory: string): Metalsmith;
        directory(): string;
        source(path: string): Metalsmith;
        source(): string;
        destination(path: string): Metalsmith;
        destination(): string;
        concurrency(max: number): Metalsmith;
        concurrency(): number;
        clean(clean: boolean): Metalsmith;
        clean(): boolean;
        frontmatter(frontmatter: boolean): Metalsmith;
        frontmatter(): boolean;
        metadata(metadata: object): Metalsmith;
        metadata(): object;
        use(plugin: Metalsmith.Plugin | Metalsmith.Plugin[]): Metalsmith;
        ignore(files: string | string[] | Metalsmith.Ignore | Metalsmith.Ignore[]): Metalsmith;
        ignore(): string[];
        path(...paths: string[]): string;
        build(fn?: Metalsmith.Callback): object;
        process(fn?: Metalsmith.Callback): object;
        run(files: object, fn?: Metalsmith.Callback): object;
        run(files: object, plugins: Metalsmith.Plugin[], fn?: Metalsmith.Callback): object;
        read(dir: string, fn?: Metalsmith.Callback): object;
        read(fn: Metalsmith.Callback): object;
        readFile(file: string): object;
        write(files: object, dir?: string, fn?: Metalsmith.Callback): void;
        write(files: object, fn: Metalsmith.Callback): void;
        writeFile(file: string, data: object): void;
        env(vars: string | Object, value: string | number | boolean): Metalsmith;
        readonly plugins: Metalsmith.Plugin[];
        readonly ignores: string[];
    }

    declare function Metalsmith(directory: string): Metalsmith;

    declare namespace Metalsmith {
        type Plugin = (files: Files, metalsmith: Metalsmith, done: DoneCallback) => void;
        type DoneCallback = (err?: Error) => void;
        type Callback = (err: Error | null, files: Files, metalsmith: Metalsmith) => void;
        type Ignore = (path: string, stat: Stats) => void;

        interface File {
            contents: Buffer;
            stats?: Stats;
            mode?: string;
            [property: string]: any;
        }

        interface Files {
            [filepath: string]: File;
        }
    }

    export = Metalsmith;
}
