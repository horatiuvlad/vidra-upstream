# Vidra.Modules.Notifications

Local notifications for [Vidra](https://vidra.build) applications on Windows
and macOS.

The generated TypeScript proxy exposes:

- `notifications.requestPermission()`
- `notifications.show({ title, body? })`

Request permission before showing a notification. On macOS, permission and
delivery require a correctly signed application.

See the [native capabilities reference](https://vidra.build/docs/reference/capabilities/native/)
and [distribution guide](https://vidra.build/docs/guides/distribution/).

[NuGet](https://www.nuget.org/packages/Vidra.Modules.Notifications) ·
[GitHub](https://github.com/rzamfiriu/vidra)
