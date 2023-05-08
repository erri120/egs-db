# Data Dumps

These data dumps have been exported from the [scraper](../scraper) and are not designed to be changed **manually**.

## Namespaces

The file [`namespaces.json`](./namespaces.json) contains a dictionary that maps catalog namespaces to URL slugs:

```json
{
  "fn": "fortnite"
}
```

This file contains **every** namespace and URL slugs can be turned into complete store URLs using this format:

```
https://store.epicgames.com/en-US/p/{url-slug}
```

## Entire Catalog

The directory [`namespaces`](./namespaces) contains a directory for every [Namespace](#namespaces). Every directory contains a file for every catalog item in the namespace:

```
└─ wren
   ├─ 26e2fb059d1e433ba91291f6789c7136.json
   ├─ b2e4a9b2f8474048bc258a23307b43d1.json
   ├─ d8af774841684f0c8d465bcfcb24d442.json
   ├─ 25dcc8aa6b174704a7ebd614706b703b.json
   ├─ 55c6294210e5469aaa21c44fa04f96ab.json
   ├─ f0cacc038f734388a2983c7b7100f0f3.json
   ├─ babd819da5f540c59eb188a25d4beabb.json
   ├─ a58322b8682f4e1d9fde15fa7b354836.json
   ├─ 4c01dfdcdcd14595a8581217f5f0a255.json
   ├─ 4dfac3cf58004135a39e92877fc227ab.json
   └─ ce04a7a8e3d6472194e2f128524eb730.json
```

It should be noted that not every catalog item is actually a game. The `fn` (Fortnite) namespace has over 300 catalog items, one of which is the actual game, and others are promotions, staging builds, DLCs/Addons and more. You can use the `categories` field to somewhat narrow down your searches. Games usually have the categories `public/games/applications`, while addons have `public/addons/applications`.
