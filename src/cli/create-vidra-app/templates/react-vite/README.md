# {{appTitle}}

A desktop application built with [Vidra](https://vidra.build): a React UI and a
C#/.NET native host.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- .NET MAUI workload: `dotnet workload install maui`
- [Node.js](https://nodejs.org/) 22 or newer
- Xcode for macOS builds

Windows targets must be built on Windows.

## Development

```bash
npm run doctor
npm run dev
```

`npm run dev` starts Vite and the native host together. Changes to the web UI
reload through Vite; supported C# changes reload through the .NET development
loop.

## Build

```bash
npm run build
```

The `vidra` CLI is a local project dependency. Run `npx vidra --help` to see
target, packaging, and update options.

## Project structure

```text
{{projectNameKebab}}/
├── src/
│   └── {{projectName}}.Host/  # .NET MAUI host and C# contracts
└── ui/                        # React application and generated TypeScript
```

Read the [Vidra documentation](https://vidra.build/docs/) for bridge guides,
capabilities, distribution, and updates.
