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
