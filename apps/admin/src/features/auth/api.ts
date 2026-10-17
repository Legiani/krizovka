import { CurrentUser, LoginRequest, LoginResponse } from './types';

export const authApi = {
  async login(data: LoginRequest): Promise<LoginResponse> {
    const res = await fetch('/api/backoffice/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });

    if (!res.ok) {
      let message = 'Neplatné přihlašovací údaje.';
      try {
        const errorData = await res.json();
        message = errorData.detail || errorData.title || errorData.message || errorData.errorMessage || message;
      } catch {
        // use fallback
      }
      throw new Error(message);
    }

    return res.json();
  },

  async logout(): Promise<void> {
    try {
      await fetch('/api/backoffice/auth/logout', {
        method: 'POST',
      });
    } catch {
      // Continue client-side logout
    }
  },

  async getCurrentUser(signal?: AbortSignal): Promise<CurrentUser | null> {
    try {
      const res = await fetch('/api/backoffice/auth/current-user', {
        signal,
        cache: 'no-store',
      });
      if (!res.ok) return null;
      return await res.json();
    } catch {
      return null;
    }
  },
};
