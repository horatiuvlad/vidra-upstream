import { defineConfig, events, native } from "@vidra-dev/sdk/config";

export default defineConfig({
  bridge: {
    allow: [
      native.app.getInfo,
      native.appWindow.center,
      native.appWindow.configure,
      native.appWindow.getCurrent,
      native.appWindow.getSupport,
      native.appWindow.maximize,
      native.appWindow.minimize,
      native.appWindow.restore,
      native.appWindow.setTitle,
      native.browser.open,
      native.clipboard.getText,
      native.notifications.requestPermission,
      native.notifications.show,
    ],
    events: [
      events.appWindow.resized,
      events.appWindow.stateChanged,
      events.runtime.hotReloaded,
    ],
  },
  updates: {
    feed: "",
  },
});
