'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { Alert, Button, Form, Spinner } from 'react-bootstrap';
import { ModulePage } from '@/shared/components/ModulePage';
import { OrganizationList } from '@/features/organizations/OrganizationList';
import { OrganizationEditor } from '@/features/organizations/OrganizationEditor';
import { fetchOrganizations, type ApiOrganization } from '@/features/organizations/api';
import { selectOrganization } from '@/demo/store';

export default function OrganizationsPage() {
  const router = useRouter();
  const [organizations, setOrganizations] = useState<ApiOrganization[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [editor, setEditor] = useState<ApiOrganization | 'new' | null>(null);
  const [notice, setNotice] = useState('');
  const [errorNotice, setErrorNotice] = useState('');

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setErrorNotice('');
      const data = await fetchOrganizations();
      setOrganizations(data);
    } catch (err) {
      setErrorNotice((err as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    Promise.resolve().then(() => {
      loadData();
    });
    return () => controller.abort();
  }, [loadData]);

  const filtered = organizations.filter(org => {
    const text = `${org.name} ${org.code} ${org.contactEmail || ''}`.toLocaleLowerCase('cs');
    const matchesSearch = text.includes(search.toLocaleLowerCase('cs').trim());
    const matchesStatus = !status || org.status.toUpperCase() === status.toUpperCase();
    return matchesSearch && matchesStatus;
  });

  return (
    <>
      <ModulePage
        title="Organizace"
        subtitle="Tenanti Krizovky · vlastní uživatelé, dispečinky, geofencing a metodiky"
        noCard
        actions={
          <Button
            onClick={() => {
              setNotice('');
              setEditor('new');
            }}
          >
            Přidat organizaci
          </Button>
        }
        filters={
          <div className="row g-3">
            <Form.Group controlId="organization-search" className="col-md-8">
              <Form.Label>Hledat organizaci</Form.Label>
              <Form.Control
                type="search"
                placeholder="Název, kód nebo e-mail"
                value={search}
                onChange={e => setSearch(e.target.value)}
              />
            </Form.Group>
            <Form.Group controlId="organization-status-filter" className="col-md-4">
              <Form.Label>Stav</Form.Label>
              <Form.Select value={status} onChange={e => setStatus(e.target.value)}>
                <option value="">Všechny stavy</option>
                <option value="ACTIVE">Aktivní</option>
                <option value="PLANNED">Připravované</option>
                <option value="INACTIVE">Neaktivní</option>
              </Form.Select>
            </Form.Group>
          </div>
        }
      >
        {notice && <Alert variant="success" role="status">{notice}</Alert>}
        {errorNotice && <Alert variant="danger" role="alert">{errorNotice}</Alert>}

        {loading ? (
          <div className="text-center py-5">
            <Spinner animation="border" role="status" variant="primary" />
            <p className="mt-2 text-secondary">Načítám organizace z API...</p>
          </div>
        ) : (
          <>
            <p className="small text-secondary" aria-live="polite">
              Zobrazeno {filtered.length} z {organizations.length} organizací
            </p>
            <OrganizationList
              organizations={filtered}
              onEdit={org => {
                setNotice('');
                setEditor(org);
              }}
              onSelect={id => {
                selectOrganization(id);
                router.push('/');
              }}
            />
          </>
        )}
      </ModulePage>

      {editor && (
        <OrganizationEditor
          key={editor === 'new' ? 'new' : editor.id}
          organization={editor === 'new' ? null : editor}
          onClose={() => setEditor(null)}
          onSaved={() => {
            setEditor(null);
            setNotice('Organizace a parametry perimetru byly úspěšně uloženy.');
            loadData();
          }}
        />
      )}
    </>
  );
}
