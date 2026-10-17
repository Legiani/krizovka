'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/features/auth/api';

export default function LoginPage() {
  return (
    <React.Suspense
      fallback={
        <div className="d-flex justify-content-center align-items-center vh-100 bg-light">
          <div className="spinner-border text-danger" role="status">
            <span className="visually-hidden">Načítání...</span>
          </div>
        </div>
      }
    >
      <LoginContent />
    </React.Suspense>
  );
}

function LoginContent() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await authApi.login({ email, password });
      router.push('/');
      router.refresh();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Neočekávaná chyba serveru. Zkuste to prosím znovu.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="d-flex justify-content-center align-items-center vh-100 bg-light">
      <div className="card shadow-sm border-0" style={{ width: '420px', maxWidth: '92vw' }}>
        <div className="card-body p-4 p-md-5">
          <div className="text-center mb-4">
            <h2 className="fw-bold text-danger mb-1">Krizovka <span className="text-dark fw-light fs-4">velín</span></h2>
            <p className="text-muted small mb-0">Přihlášení operátora a administrátora</p>
          </div>

          {error && <div className="alert alert-danger py-2 small" role="alert">{error}</div>}

          <div className="d-flex gap-2 mb-3">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm flex-fill"
              onClick={() => { setEmail('operator@krizovka.cz'); setPassword('Operator123!'); }}
            >
              Operátor velínu
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm flex-fill"
              onClick={() => { setEmail('superadmin@krizovka.cz'); setPassword('SuperAdmin123!'); }}
            >
              Super Administrátor
            </button>
          </div>

          <form onSubmit={handleLogin}>
            <div className="mb-3">
              <label className="form-label small fw-bold" htmlFor="login-email">Pracovní email</label>
              <input
                id="login-email"
                type="email"
                className="form-control"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="operator@krizovka.cz"
                autoComplete="username"
                required
              />
            </div>
            <div className="mb-4">
              <label className="form-label small fw-bold" htmlFor="login-password">Heslo</label>
              <input
                id="login-password"
                type="password"
                className="form-control"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
              />
            </div>
            <button
              type="submit"
              className="btn btn-danger w-100 py-2 fw-semibold"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true" />
                  Přihlašování...
                </>
              ) : (
                'Přihlásit se do velínu'
              )}
            </button>
          </form>
        </div>
        <div className="card-footer bg-light text-center py-3 border-top-0">
          <small className="text-muted">&copy; Krizovka · Záchranný systém</small>
        </div>
      </div>
    </div>
  );
}
