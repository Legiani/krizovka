'use client';

import { useRef, useState } from 'react';
import { Alert, Button, Form, Modal } from 'react-bootstrap';
import type { ApiOrganization } from './api';
import { createOrganization, updateOrganization, upsertOrganizationArea } from './api';
import { saveOrganization } from '@/demo/store';

export function OrganizationEditor({ organization, onClose, onSaved }: {
  organization: ApiOrganization | null; onClose: () => void; onSaved: () => void;
}) {
  const [form, setForm] = useState({
    name: organization?.name ?? '',
    code: organization?.code ?? '',
    contactEmail: organization?.contactEmail ?? '',
    contactPhone: organization?.contactPhone ?? '',
    status: organization?.status ?? 'Active',
    isDefaultDispatch: organization?.isDefaultDispatch ?? false,
    areaName: organization?.areas?.[0]?.name ?? '',
    polygonGeoJson: organization?.areas?.[0]?.polygonGeoJson ?? '',
    offsetMeters: organization?.areas?.[0]?.offsetMeters ?? 50,
    allocatedVelinId: organization?.areas?.[0]?.allocatedVelinId ?? 'default-velin',
  });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const errorRef = useRef<HTMLDivElement>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');
    if (!form.name.trim() || !form.code.trim()) {
      setError('Vyplňte název a kód organizace.');
      return;
    }

    setSaving(true);
    try {
      let savedOrg: ApiOrganization;
      if (organization?.id) {
        savedOrg = await updateOrganization(organization.id, {
          name: form.name.trim(),
          code: form.code.trim().toUpperCase(),
          status: form.status,
          contactEmail: form.contactEmail.trim() || undefined,
          contactPhone: form.contactPhone.trim() || undefined,
          isDefaultDispatch: form.isDefaultDispatch,
        });

        if (form.areaName.trim() && form.polygonGeoJson.trim()) {
          await upsertOrganizationArea(organization.id, {
            name: form.areaName.trim(),
            polygonGeoJson: form.polygonGeoJson.trim(),
            offsetMeters: Number(form.offsetMeters) || 0,
            allocatedVelinId: form.allocatedVelinId.trim() || 'default-velin',
          });
        }
      } else {
        savedOrg = await createOrganization({
          name: form.name.trim(),
          code: form.code.trim().toUpperCase(),
          status: form.status,
          contactEmail: form.contactEmail.trim() || undefined,
          contactPhone: form.contactPhone.trim() || undefined,
          isDefaultDispatch: form.isDefaultDispatch,
        });

        if (form.areaName.trim() && form.polygonGeoJson.trim()) {
          await upsertOrganizationArea(savedOrg.id, {
            name: form.areaName.trim(),
            polygonGeoJson: form.polygonGeoJson.trim(),
            offsetMeters: Number(form.offsetMeters) || 0,
            allocatedVelinId: form.allocatedVelinId.trim() || 'default-velin',
          });
        }
      }

      // Sync with local demo store for compatibility with user management
      saveOrganization({
        id: savedOrg.id,
        name: savedOrg.name,
        code: savedOrg.code,
        contactEmail: savedOrg.contactEmail || '',
        status: (savedOrg.status.toUpperCase() === 'ACTIVE' ? 'ACTIVE' : savedOrg.status.toUpperCase() === 'PLANNED' ? 'PLANNED' : 'INACTIVE'),
      });

      onSaved();
    } catch (caught) {
      setError((caught as Error).message);
      requestAnimationFrame(() => errorRef.current?.focus());
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal show onHide={onClose} size="lg" centered aria-labelledby="organization-editor-title">
      <Modal.Header closeButton closeLabel="Zavřít">
        <Modal.Title id="organization-editor-title">{organization ? 'Upravit organizaci a perimetr' : 'Přidat organizaci a perimetr'}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {error && <Alert variant="danger" role="alert" tabIndex={-1} ref={errorRef}>{error}</Alert>}
          <div className="row g-3">
            <Form.Group controlId="org-name" className="col-md-8">
              <Form.Label>Název organizace</Form.Label>
              <Form.Control autoFocus required maxLength={160} value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
            </Form.Group>
            <Form.Group controlId="org-code" className="col-md-4">
              <Form.Label>Kód organizace</Form.Label>
              <Form.Control required pattern="[A-Za-z0-9_-]+" maxLength={32} value={form.code} onChange={e => setForm({ ...form, code: e.target.value.toUpperCase() })} aria-describedby="org-code-help" />
              <Form.Text id="org-code-help">Unikátní identifikátor (např. KAMPUS).</Form.Text>
            </Form.Group>
            <Form.Group controlId="org-email" className="col-md-6">
              <Form.Label>Kontaktní e-mail</Form.Label>
              <Form.Control type="email" maxLength={254} value={form.contactEmail} onChange={e => setForm({ ...form, contactEmail: e.target.value })} />
            </Form.Group>
            <Form.Group controlId="org-phone" className="col-md-6">
              <Form.Label>Telefon velínu</Form.Label>
              <Form.Control type="tel" maxLength={50} value={form.contactPhone} onChange={e => setForm({ ...form, contactPhone: e.target.value })} />
            </Form.Group>
            <Form.Group controlId="org-status" className="col-md-6">
              <Form.Label>Stav</Form.Label>
              <Form.Select value={form.status} onChange={e => setForm({ ...form, status: e.target.value })}>
                <option value="Active">Aktivní</option>
                <option value="Planned">Připravovaná</option>
                <option value="Inactive">Neaktivní</option>
              </Form.Select>
            </Form.Group>
            <Form.Group controlId="org-default-dispatch" className="col-md-6 d-flex align-items-center mt-4">
              <Form.Check
                type="checkbox"
                id="check-default-dispatch"
                label="Výchozí dispečink (Fallback Velín pro neznámé lokality)"
                checked={form.isDefaultDispatch}
                onChange={e => setForm({ ...form, isDefaultDispatch: e.target.checked })}
              />
            </Form.Group>
          </div>

          <hr className="my-4" />
          <h6 className="fw-bold mb-3">📍 Geofencing & Perimetr areálu</h6>
          <div className="row g-3">
            <Form.Group controlId="area-name" className="col-md-6">
              <Form.Label>Název areálu / budovy</Form.Label>
              <Form.Control placeholder="např. Kampus Sever - Hlavní budova" value={form.areaName} onChange={e => setForm({ ...form, areaName: e.target.value })} />
            </Form.Group>
            <Form.Group controlId="area-velin" className="col-md-6">
              <Form.Label>Alokovaný velín ID</Form.Label>
              <Form.Control placeholder="např. velin-kampus-sever" value={form.allocatedVelinId} onChange={e => setForm({ ...form, allocatedVelinId: e.target.value })} />
            </Form.Group>
            <Form.Group controlId="area-offset" className="col-md-4">
              <Form.Label>Offset / Buffer (metry)</Form.Label>
              <Form.Control type="number" min="0" max="500" value={form.offsetMeters} onChange={e => setForm({ ...form, offsetMeters: Number(e.target.value) })} />
              <Form.Text>Toleranční zóna pro parkoviště a vstupní brány (výchozí 50 m).</Form.Text>
            </Form.Group>
            <Form.Group controlId="area-polygon" className="col-md-8">
              <Form.Label>GeoJSON Polygon hranice</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                placeholder='{"type":"Polygon","coordinates":[[[lon,lat],...]]}'
                value={form.polygonGeoJson}
                onChange={e => setForm({ ...form, polygonGeoJson: e.target.value })}
              />
              <Form.Text>Zadejte platný GeoJSON polygon se souřadnicemi.</Form.Text>
            </Form.Group>
          </div>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="outline-secondary" onClick={onClose} disabled={saving}>Zrušit</Button>
          <Button type="submit" disabled={saving}>
            {saving ? 'Ukládám...' : 'Uložit organizaci'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}
