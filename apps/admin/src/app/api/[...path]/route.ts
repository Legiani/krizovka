import { NextRequest } from 'next/server';

export const dynamic = 'force-dynamic';

// The destination is read at runtime, so one image works locally and in Kubernetes.
async function proxy(request: NextRequest) {
  const base = process.env.API_INTERNAL_URL || 'http://127.0.0.1:5080';
  const url = new URL(base);
  url.pathname = request.nextUrl.pathname;
  url.search = request.nextUrl.search;
  const headers = new Headers();
  for (const name of ['authorization', 'content-type', 'accept']) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  try {
    const response = await fetch(url, {
      method: request.method,
      headers,
      body: ['GET', 'HEAD'].includes(request.method) ? undefined : await request.arrayBuffer(),
      cache: 'no-store',
      redirect: 'manual',
      signal: AbortSignal.timeout(15_000),
    });
    const responseHeaders = new Headers({ 'cache-control': 'no-store' });
    for (const name of ['content-type', 'www-authenticate']) {
      const value = response.headers.get(name);
      if (value) responseHeaders.set(name, value);
    }
    return new Response(response.body, { status: response.status, headers: responseHeaders });
  } catch {
    return Response.json({ title: 'Backend není dostupný.' }, { status: 502 });
  }
}

export { proxy as GET, proxy as POST, proxy as PUT, proxy as PATCH, proxy as DELETE };
