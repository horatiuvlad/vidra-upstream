export const NATIVE_METHOD_TOKEN = "vidra.native-method" as const;
export const EVENT_TOKEN = "vidra.event" as const;

export interface NativeMethodToken {
  readonly kind: typeof NATIVE_METHOD_TOKEN;
  readonly contract: string;
  readonly member: string;
}

export interface EventToken {
  readonly kind: typeof EVENT_TOKEN;
  readonly contract: string;
  readonly member: string;
}

/** @internal Used by generated contract catalogs. */
export const nativeMethodToken = (
  contract: string,
  member: string,
): NativeMethodToken =>
  Object.freeze({ kind: NATIVE_METHOD_TOKEN, contract, member });

/** @internal Used by generated contract catalogs. */
export const eventToken = (contract: string, member: string): EventToken =>
  Object.freeze({ kind: EVENT_TOKEN, contract, member });
