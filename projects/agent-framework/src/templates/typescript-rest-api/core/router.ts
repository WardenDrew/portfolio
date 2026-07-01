import { randomUUID } from "node:crypto";
import type { IncomingMessage, ServerResponse } from "node:http";
import { HttpError, type Handler, type HttpMethod, type Middleware, type RequestContext } from "./http.js";

type Route = {
  method: HttpMethod;
  pattern: string;
  segments: string[];
  handler: Handler;
};

export class Router {
  private readonly routes: Route[] = [];
  private readonly middleware: Middleware[] = [];

  use(middleware: Middleware): void {
    this.middleware.push(middleware);
  }

  get(pattern: string, ...handlers: [...Middleware[], Handler]): void {
    this.add("GET", pattern, handlers);
  }

  post(pattern: string, ...handlers: [...Middleware[], Handler]): void {
    this.add("POST", pattern, handlers);
  }

  put(pattern: string, ...handlers: [...Middleware[], Handler]): void {
    this.add("PUT", pattern, handlers);
  }

  patch(pattern: string, ...handlers: [...Middleware[], Handler]): void {
    this.add("PATCH", pattern, handlers);
  }

  delete(pattern: string, ...handlers: [...Middleware[], Handler]): void {
    this.add("DELETE", pattern, handlers);
  }

  async handle(request: IncomingMessage, response: ServerResponse): Promise<void> {
    const method = normalizeMethod(request.method);
    const url = new URL(request.url ?? "/", "http://localhost");

    if (!method) {
      response.writeHead(405).end();
      return;
    }

    const match = this.match(method, url.pathname);
    if (!match) {
      response.writeHead(404, { "content-type": "application/json; charset=utf-8" }).end(JSON.stringify({ error: "Not found" }));
      return;
    }

    const context: RequestContext = {
      request,
      response,
      method,
      url,
      params: match.params,
      requestId: request.headers["x-request-id"]?.toString() ?? randomUUID(),
    };

    const handler = [...this.middleware, ...match.middleware].reduceRight<Handler>(
      (next, middleware) => middleware(next),
      match.handler,
    );

    try {
      await handler(context);
      if (!response.writableEnded) {
        response.writeHead(204, { "x-request-id": context.requestId }).end();
      }
    } catch (error) {
      writeError(context, error);
    }
  }

  private add(method: HttpMethod, pattern: string, handlers: [...Middleware[], Handler]): void {
    const handler = handlers.at(-1);
    if (!handler) {
      throw new Error(`Route ${method} ${pattern} requires a handler`);
    }
    const middleware = handlers.slice(0, -1) as Middleware[];
    const composed = middleware.reduceRight<Handler>((next, item) => item(next), handler as Handler);
    this.routes.push({
      method,
      pattern,
      segments: splitPath(pattern),
      handler: composed,
    });
  }

  private match(method: HttpMethod, pathname: string): { handler: Handler; middleware: Middleware[]; params: Record<string, string> } | undefined {
    const pathSegments = splitPath(pathname);
    for (const route of this.routes) {
      if (route.method !== method || route.segments.length !== pathSegments.length) {
        continue;
      }

      const params: Record<string, string> = {};
      let matched = true;
      for (let index = 0; index < route.segments.length; index += 1) {
        const patternSegment = route.segments[index];
        const pathSegment = pathSegments[index];
        if (patternSegment?.startsWith(":")) {
          params[patternSegment.slice(1)] = decodeURIComponent(pathSegment ?? "");
        } else if (patternSegment !== pathSegment) {
          matched = false;
          break;
        }
      }

      if (matched) {
        return { handler: route.handler, middleware: [], params };
      }
    }

    return undefined;
  }
}

function normalizeMethod(method: string | undefined): HttpMethod | undefined {
  if (method === "GET" || method === "POST" || method === "PUT" || method === "PATCH" || method === "DELETE") {
    return method;
  }
  return undefined;
}

function splitPath(pathname: string): string[] {
  return pathname.split("/").filter(Boolean);
}

function writeError(context: RequestContext, error: unknown): void {
  const status = error instanceof HttpError ? error.status : 500;
  const message = error instanceof Error ? error.message : "Internal server error";
  const payload = JSON.stringify({ error: message, requestId: context.requestId });
  context.response.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "content-length": Buffer.byteLength(payload),
    "x-request-id": context.requestId,
  });
  context.response.end(payload);
}
