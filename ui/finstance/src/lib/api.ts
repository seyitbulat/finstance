import "server-only";
const base = process.env.FINSTANCE_API_URL ?? "http://localhost:5257";
export async function forward(
  request: Request,
  path: string,
  init: RequestInit = {},
) {
  try {
    const response = await fetch(new URL(path, base), {
      ...init,
      headers: {
        "X-User-Name": request.headers.get("X-User-Name") || "guest",
        ...init.headers,
      },
      cache: "no-store",
      signal: AbortSignal.timeout(60_000),
    });
    return new Response(await response.text(), {
      status: response.status,
      headers: {
        "Content-Type":
          response.headers.get("Content-Type") ?? "application/json",
        "Cache-Control": "no-store",
      },
    });
  } catch {
    return Response.json(
      {
        error:
          "API’ye ulaşılamadı. Geliştirme ortamındaki API ve veritabanının çalıştığını kontrol edin.",
      },
      { status: 502 },
    );
  }
}
