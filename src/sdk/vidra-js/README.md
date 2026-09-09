# @vidra-dev/sdk

Framework-agnostic TypeScript SDK for the
[Vidra](https://vidra.build) bridge.

> **Alpha:** APIs may change between 0.x releases.

## Install

```bash
npm install @vidra-dev/sdk
```

Most applications should start with
[`create-vidra-app`](https://www.npmjs.com/package/create-vidra-app), which
configures the SDK and generated app contracts automatically.

## Usage

```ts
import { appWindow, clipboard, connectivity, vidra } from "@vidra-dev/sdk";

const { text } = await clipboard.getText();
const currentWindow = await appWindow.getCurrent();

const unsubscribe = connectivity.onChanged(status => {
  console.log(status.access);
});

const capabilities = await vidra.capabilities();
```

Built-in native methods and events are generated from C# contracts, providing
typed payloads, results, and editor completion. App-owned contracts are
generated into the application.

## Documentation

- [Getting started](https://vidra.build/docs/getting-started/)
- [Capabilities](https://vidra.build/docs/reference/capabilities/)
- [Code generation](https://vidra.build/docs/bridge/code-generation/)
- [Bridge protocol](https://vidra.build/docs/bridge/protocol/)

## License

MIT
