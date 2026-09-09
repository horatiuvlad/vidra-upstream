# Updates

The full updates guide is published at
[vidra.build/docs/guides/updates](https://vidra.build/docs/guides/updates/).

The current CLI uses one feed setting for web and whole-app updates:

```ts
// vidra.config.ts
import { defineConfig } from "@vidra-dev/sdk/config";

export default defineConfig({
  updates: {
    feed: "https://updates.example.com/my-app/",
  },
});
```

```bash
npx vidra updates init --feed https://updates.example.com/my-app/
npx vidra build         # app and every configured update tier
npx vidra build --web   # web bundle only
npx vidra build --app   # installable app only
```

Run `npx vidra updates` to inspect the configuration and
`npx vidra build --help` for all release options.

> Older `vidra bundle` and `--native-update` examples have been replaced by
> `vidra build --web` and `vidra build --app`.
