# Distribution

The full signing, notarization, and packaging guide is published at
[vidra.build/docs/guides/distribution](https://vidra.build/docs/guides/distribution/).

```bash
npx vidra build          # build the app and configured update tiers
npx vidra build --plan   # preview the build
npx vidra verify         # verify the newest artifact
```

Vidra produces a macOS `.dmg` or a self-contained Windows `.zip`. Signing and
notarization are optional; unconfigured steps are skipped with a warning.

> The current whole-app update flag is `--app`. Older `--native-update`
> examples no longer apply.
