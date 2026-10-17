'use client';

import { useRef, useState } from 'react';
import { Alert, Button, Col, Form, Modal, Row } from 'react-bootstrap';
import { saveUser, useDemo } from '@/demo/store';
import { roleLabels, type DemoUser, type Role } from '@/demo/types';

export function UserEditor({ user, onClose, onSaved }: {
  user: DemoUser | null; onClose: () => void; onSaved: () => void;
}) {
  const { state } = useDemo();
  const [form, setForm] = useState<DemoUser>(user ?? {
    id: '', firstName: '', lastName: '', email: '', phone: '', personnelNumber: '', isActive: true,
    memberships: [{ organizationId: state.activeOrganizationId, role: 'USER' }],
  });
  const [error, setError] = useState('');
  const errorRef = useRef<HTMLDivElement>(null);
  const changeMembership = (organizationId: string, checked: boolean) => setForm({ ...form,
    memberships: checked ? [...form.memberships, { organizationId, role: 'USER' }] : form.memberships.filter(item => item.organizationId !== organizationId),
  });
  return (
    <Modal show onHide={onClose} centered size="lg" scrollable aria-labelledby="user-editor-title">
      <Modal.Header closeButton closeLabel="Zavřít"><Modal.Title id="user-editor-title">{user ? 'Upravit uživatele' : 'Přidat uživatele'}</Modal.Title></Modal.Header>
      <Form onSubmit={event => {
        event.preventDefault();
        try {
          if (!form.firstName.trim() || !form.lastName.trim()) throw new Error('Vyplňte jméno a příjmení.');
          saveUser({ ...form, id: form.id || crypto.randomUUID(), firstName: form.firstName.trim(), lastName: form.lastName.trim(), email: form.email.trim().toLowerCase(), phone: form.phone.trim(), personnelNumber: form.personnelNumber.trim() });
          onSaved();
        } catch (caught) {
          setError((caught as Error).message);
          requestAnimationFrame(() => errorRef.current?.focus());
        }
      }}>
        <Modal.Body>
          {error && <Alert variant="danger" role="alert" tabIndex={-1} ref={errorRef}>{error}</Alert>}
          <Row className="g-3 mb-3">
            <Col sm={6}><Form.Group controlId="user-first-name"><Form.Label>Jméno</Form.Label><Form.Control autoFocus required maxLength={80} autoComplete="given-name" value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} /></Form.Group></Col>
            <Col sm={6}><Form.Group controlId="user-last-name"><Form.Label>Příjmení</Form.Label><Form.Control required maxLength={80} autoComplete="family-name" value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} /></Form.Group></Col>
            <Col sm={6}><Form.Group controlId="user-email"><Form.Label>E-mail</Form.Label><Form.Control type="email" required maxLength={254} autoComplete="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} /></Form.Group></Col>
            <Col sm={6}><Form.Group controlId="user-phone"><Form.Label>Telefon <span className="text-secondary">(volitelné)</span></Form.Label><Form.Control type="tel" autoComplete="tel" maxLength={32} value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} /></Form.Group></Col>
            <Col sm={6}><Form.Group controlId="user-number"><Form.Label>Osobní číslo <span className="text-secondary">(volitelné)</span></Form.Label><Form.Control maxLength={40} value={form.personnelNumber} onChange={e => setForm({ ...form, personnelNumber: e.target.value })} /></Form.Group></Col>
          </Row>
          <fieldset className="membership-fields">
            <legend className="h6">Členství a role v organizacích</legend>
            <p className="small text-secondary">Jeden uživatel může mít v každé organizaci jinou roli.</p>
            {state.organizations.map(org => {
              const membership = form.memberships.find(item => item.organizationId === org.id);
              return <div key={org.id} className="membership-row">
                <Form.Check id={`member-${org.id}`} label={org.name} checked={!!membership} onChange={e => changeMembership(org.id, e.target.checked)} />
                {membership && <Form.Select aria-label={`Role v organizaci ${org.name}`} value={membership.role} onChange={e => setForm({ ...form, memberships: form.memberships.map(item => item.organizationId === org.id ? { ...item, role: e.target.value as Role } : item) })}>
                  {Object.entries(roleLabels).map(([role, label]) => <option key={role} value={role}>{label}</option>)}
                </Form.Select>}
              </div>;
            })}
          </fieldset>
          <Form.Check className="mt-3" id="user-active" label="Aktivní uživatel" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} />
          <p className="small text-secondary mt-3 mb-0">Demo ukládá záznam v prohlížeči. Nevytváří přihlášení ani neodesílá pozvánku. Osobní číslo a telefon zde slouží jen jako ukázka.</p>
        </Modal.Body>
        <Modal.Footer><Button variant="outline-secondary" onClick={onClose}>Zrušit</Button><Button type="submit">Uložit uživatele</Button></Modal.Footer>
      </Form>
    </Modal>
  );
}
