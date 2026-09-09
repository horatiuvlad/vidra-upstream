import { describe, expect, it } from "vitest";
import {
  defineConfig,
  events,
  filesystemRoots,
  native,
  within,
} from "../config.js";

describe("typed Vidra configuration", () => {
  it("exposes built-in modules and members as generated tokens", () => {
    expect(native.clipboard.getText).toEqual({
      kind: "vidra.native-method",
      contract: "clipboard",
      member: "getText",
    });
    expect(events.appWindow.resized).toEqual({
      kind: "vidra.event",
      contract: "appWindow",
      member: "resized",
    });
  });

  it("builds symbolic filesystem child roots", () => {
    expect(within(filesystemRoots.appData, "notes/attachments")).toEqual({
      kind: "vidra.filesystem-root",
      name: "appData",
      relativePath: "notes/attachments",
    });
  });

  it("returns config exports unchanged", () => {
    const config = { updates: { feed: "" } } as const;
    expect(defineConfig(config)).toBe(config);
  });
});
