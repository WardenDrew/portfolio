import { createHmac, timingSafeEqual } from "node:crypto";
import { HttpError } from "../../core/http.js";
import type { JwtClaims, JwtVerifier } from "../../core/auth.js";

export function createHs256JwtVerifier(secret: string): JwtVerifier {
  if (!secret) {
    throw new Error("JWT_HS256_SECRET is required");
  }

  return {
    async verify(token: string): Promise<JwtClaims> {
      const [encodedHeader, encodedPayload, signature] = token.split(".");
      if (!encodedHeader || !encodedPayload || !signature) {
        throw new HttpError(401, "Token format is invalid");
      }

      const header = parseJson(base64UrlDecode(encodedHeader)) as { alg?: string; typ?: string };
      if (header.alg !== "HS256") {
        throw new HttpError(401, "Unsupported token algorithm");
      }

      const expected = sign(`${encodedHeader}.${encodedPayload}`, secret);
      if (!safeEqual(signature, expected)) {
        throw new HttpError(401, "Token signature is invalid");
      }

      const claims = parseJson(base64UrlDecode(encodedPayload)) as JwtClaims;
      const expiresAt = typeof claims.exp === "number" ? claims.exp : undefined;
      if (expiresAt && Math.floor(Date.now() / 1000) >= expiresAt) {
        throw new HttpError(401, "Token has expired");
      }

      return claims;
    },
  };
}

function sign(value: string, secret: string): string {
  return createHmac("sha256", secret).update(value).digest("base64url");
}

function base64UrlDecode(value: string): string {
  return Buffer.from(value, "base64url").toString("utf8");
}

function parseJson(value: string): unknown {
  try {
    return JSON.parse(value) as unknown;
  } catch {
    throw new HttpError(401, "Token payload is invalid");
  }
}

function safeEqual(left: string, right: string): boolean {
  const leftBuffer = Buffer.from(left);
  const rightBuffer = Buffer.from(right);
  return leftBuffer.length === rightBuffer.length && timingSafeEqual(leftBuffer, rightBuffer);
}
