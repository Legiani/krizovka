'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { Form, Button } from 'react-bootstrap';
import { selectOrganization, resetDemo, useDemo } from '@/demo/store';
import { authApi } from '@/features/auth/api';
import type { CurrentUser } from '@/features/auth/types';
import { ConfirmModal } from './ConfirmModal';

const links = [
  ['/', 'Hlášení'],
  ['/metodiky', 'Metodiky'],
  ['/uzivatele', 'Uživatelé'],
  ['/role', 'Role'],
  ['/organizace', 'Organizace'],
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const { state, storageFailed } = useDemo();
  const path = usePathname();
  const router = useRouter();
  const [resetOpen, setResetOpen] = useState(false);
  const [notice, setNotice] = useState('');
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    if (path === '/login') return;
    const controller = new AbortController();
    authApi.getCurrentUser(controller.signal)
      .then(user => {
        setCurrentUser(user);
        if (user && !user.isSuperAdmin && user.organizationId) {
          selectOrganization(user.organizationId);
        }
      })
      .catch(() => setCurrentUser(null));
    return () => controller.abort();
  }, [path]);

  if (path === '/login') {
    return <main id="content">{children}</main>;
  }

  const handleLogout = async () => {
    await authApi.logout();
    router.push('/login');
    router.refresh();
  };

  return (
    <>
      <a className="visually-hidden-focusable" href="#content">Přejít na obsah</a>
      <header className="app-header">
        <Link className="brand" href="/">Krizovka <span>velín</span></Link>
        <nav aria-label="Hlavní navigace">
          {links.map(([href, label]) => (
            <Link key={href} href={href} aria-current={path === href ? 'page' : undefined}>
              {label}
            </Link>
          ))}
        </nav>
        <div className="d-flex align-items-center gap-3 ms-auto flex-wrap">
          {currentUser?.isSuperAdmin ? (
            <Form.Group controlId="active-organization" className="organization-switcher mb-0">
              <Form.Label className="visually-hidden">Aktivní organizace</Form.Label>
              <Form.Select
                size="sm"
                value={state.activeOrganizationId}
                onChange={event => {
                  selectOrganization(event.target.value);
                  setNotice('');
                }}
              >
                {state.organizations.map(org => (
                  <option key={org.id} value={org.id}>
                    {org.name}{org.status !== 'ACTIVE' ? ' · ' + (org.status === 'PLANNED' ? 'připravovaná' : 'neaktivní') : ''}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>
          ) : (
            <span className="badge bg-light text-dark border py-2 px-3">
              {state.organizations.find(o => o.id === state.activeOrganizationId)?.name || 'Organizace'}
            </span>
          )}

          {currentUser ? (
            <div className="d-flex align-items-center gap-2">
              <span className="small text-muted" title={currentUser.email}>
                {currentUser.firstName ? `${currentUser.firstName} ${currentUser.lastName}` : currentUser.email}
                {currentUser.role?.name && <span className="badge bg-secondary ms-1">{currentUser.role.name}</span>}
              </span>
              <Button variant="outline-danger" size="sm" onClick={handleLogout}>
                Odhlásit
              </Button>
            </div>
          ) : (
            <Button variant="outline-secondary" size="sm" onClick={handleLogout}>
              Odhlásit
            </Button>
          )}
        </div>
      </header>

      <div className="development-notice">
        <div>
          <strong>Režim velínu</strong>
          <span className="demo-separator"> · </span>
          Příjem hlášení a autorizace velínu jsou aktivní.
        </div>
        <Button variant="outline-secondary" size="sm" onClick={() => setResetOpen(true)}>
          Obnovit ukázková data
        </Button>
      </div>

      {storageFailed && (
        <div className="alert alert-warning m-3" role="status">
          Úložiště prohlížeče není dostupné. Změny zůstanou jen do obnovení stránky.
        </div>
      )}
      {notice && <div className="alert alert-success m-3" role="status">{notice}</div>}

      <main id="content" key={state.activeOrganizationId}>{children}</main>

      <ConfirmModal
        show={resetOpen}
        onHide={() => setResetOpen(false)}
        title="Obnovit ukázková data"
        message="Změny ukázkových organizací, uživatelů a hlášení v tomto prohlížeči se nahradí výchozí sadou."
        confirmLabel="Obnovit data"
        confirmVariant="primary"
        onConfirm={() => {
          resetDemo();
          setResetOpen(false);
          setNotice('Výchozí ukázková data byla obnovena.');
        }}
      />
    </>
  );
}
