import { NextRequest } from 'next/server';

export const dynamic = 'force-dynamic';

// The destinations are read at runtime, so one image works locally and in Kubernetes.
async function proxy(request: NextRequest) {
  const pathname = request.nextUrl.pathname;
  const isUsersRoute = pathname.startsWith('/api/backoffice/') ||
                       pathname.startsWith('/api/us/') ||
                       pathname.startsWith('/api/v1/us/');

  let url: URL;
  if (isUsersRoute) {
    const usersBase = process.env.USERS_INTERNAL_URL || 'http://127.0.0.1:5081';
    url = new URL(usersBase);
    if (pathname.startsWith('/api/backoffice/')) {
      url.pathname = '/v1/us/backoffice/' + pathname.slice('/api/backoffice/'.length);
    } else if (pathname.startsWith('/api/v1/us/')) {
      url.pathname = '/v1/us/' + pathname.slice('/api/v1/us/'.length);
    } else if (pathname.startsWith('/api/us/')) {
      url.pathname = '/v1/us/' + pathname.slice('/api/us/'.length);
    }
  } else {
    const apiBase = process.env.API_INTERNAL_URL || 'http://127.0.0.1:5080';
    url = new URL(apiBase);
    url.pathname = pathname;
  }
  url.search = request.nextUrl.search;

  const headers = new Headers();
  for (const name of ['authorization', 'content-type', 'accept', 'cookie']) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }

  // Also extract Bearer from admin_session cookie if authorization header is absent
  if (!headers.has('authorization')) {
    const cookieHeader = request.headers.get('cookie');
    if (cookieHeader) {
      const match = cookieHeader.match(/(?:^|;\s*)admin_session=([^;]+)/);
      if (match && match[1]) {
        headers.set('authorization', `Bearer ${match[1]}`);
      }
    }
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
    // Forward Set-Cookie (strip Secure flag on plain HTTP localhost for browser compatibility)
    const sanitizeCookie = (c: string) => {
      if (!request.nextUrl.protocol.startsWith('https')) {
        return c.replace(/;\s*secure/gi, '');
      }
      return c;
    };

    if (typeof response.headers.getSetCookie === 'function') {
      for (const cookie of response.headers.getSetCookie()) {
        responseHeaders.append('set-cookie', sanitizeCookie(cookie));
      }
    } else {
      const setCookie = response.headers.get('set-cookie');
      if (setCookie) responseHeaders.set('set-cookie', sanitizeCookie(setCookie));
    }

    return new Response(response.body, { status: response.status, headers: responseHeaders });
  } catch {
    return Response.json({ title: 'Backend není dostupný.' }, { status: 502 });
  }
}

export { proxy as GET, proxy as POST, proxy as PUT, proxy as PATCH, proxy as DELETE };
