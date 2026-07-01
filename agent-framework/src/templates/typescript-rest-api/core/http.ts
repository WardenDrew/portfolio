import type { IncomingMessage, ServerResponse } from "node:http";

export type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export type Principal = {
  subject: string;
  scopes: Set<string>;
  claims: Record<string, unknown>;
};

export type RequestContext = {
  request: IncomingMessage;
  response: ServerResponse;
  method: HttpMethod;
  url: URL;
  params: Record<string, string>;
  requestId: string;
  principal?: Principal;
};

export type HandlerResult = void | { handled: true };
export type Handler = (context: RequestContext) => Promise<HandlerResult> | HandlerResult;
export type Middleware = (next: Handler) => Handler;

export class HttpError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly details?: unknown,
  ) {
    super(message);
  }
}

export function json(context: RequestContext, status: number, body: unknown): HandlerResult {
  const payload = JSON.stringify(body);
  context.response.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "content-length": Buffer.byteLength(payload),
    "x-request-id": context.requestId,
  });
  context.response.end(payload);
  return { handled: true };
}

export async function readJson<T>(context: RequestContext): Promise<T> {
  const chunks: Buffer[] = [];
  for await (const chunk of context.request) {
    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
  }

  if (chunks.length === 0) {
    throw new HttpError(400, "Request body is required");
  }

  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8")) as T;
  } catch {
    throw new HttpError(400, "Request body must be valid JSON");
  }
}
