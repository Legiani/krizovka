'use client';

import React, { useState } from 'react';
import { Modal, Button, Form, Alert, Row, Col } from 'react-bootstrap';
import { CreateRoleRequest, AddPermissionRequest } from '../types';
import { PERMISSION_CATALOG } from '../permissionCatalog';

// CREATE ROLE MODAL
export function CreateRoleModal({
  show,
  onClose,
  onCreate,
}: {
  show: boolean;
  onClose: () => void;
  onCreate: (data: CreateRoleRequest) => Promise<void>;
}) {
  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !code.trim()) {
      setError('Vyplňte prosím název i kód role.');
      return;
    }
    const cleanCode = code.trim().toUpperCase().replace(/[^A-Z0-9_]/g, '_');
    setError('');
    setLoading(true);
    try {
      await onCreate({
        name: name.trim(),
        code: cleanCode,
        description: description.trim() || undefined,
      });
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při vytváření role.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal show={show} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>Vytvořit novou roli</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {error && <Alert variant="danger">{error}</Alert>}
          <Form.Group className="mb-3" controlId="role-name">
            <Form.Label>Název role</Form.Label>
            <Form.Control
              required
              autoFocus
              placeholder="např. Operátor školy"
              value={name}
              onChange={e => setName(e.target.value)}
            />
          </Form.Group>
          <Form.Group className="mb-3" controlId="role-code">
            <Form.Label>Kód role (UPPER_CASE)</Form.Label>
            <Form.Control
              required
              placeholder="např. OPERATOR_SKOLY"
              value={code}
              onChange={e => setCode(e.target.value.toUpperCase())}
            />
            <Form.Text className="text-muted">Pouze velká písmena, čísla a podtržítka.</Form.Text>
          </Form.Group>
          <Form.Group className="mb-3" controlId="role-description">
            <Form.Label>Popis (volitelné)</Form.Label>
            <Form.Control
              as="textarea"
              rows={2}
              placeholder="Stručný účel a rozsah této role"
              value={description}
              onChange={e => setDescription(e.target.value)}
            />
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="outline-secondary" onClick={onClose} disabled={loading}>
            Zrušit
          </Button>
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? 'Vytvářím...' : 'Vytvořit roli'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

// ADD PERMISSION MODAL
export function AddPermissionModal({
  show,
  roleName,
  onClose,
  onAdd,
}: {
  show: boolean;
  roleName?: string;
  onClose: () => void;
  onAdd: (data: AddPermissionRequest) => Promise<void>;
}) {
  const [selectedModule, setSelectedModule] = useState<string>(PERMISSION_CATALOG[0]?.code || '');
  const [selectedAction, setSelectedAction] = useState<string>('READ');
  const [scopeType, setScopeType] = useState<string>('GLOBAL');
  const [scopeValue, setScopeValue] = useState<string>('*');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const activeModule = PERMISSION_CATALOG.find(m => m.code === selectedModule);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedModule || !selectedAction) {
      setError('Vyberte modul a akci.');
      return;
    }
    setError('');
    setLoading(true);
    try {
      await onAdd({
        module: selectedModule,
        action: selectedAction,
        scopeType,
        scopeValue: scopeType === 'GLOBAL' ? '*' : scopeValue.trim(),
      });
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při přidávání oprávnění.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal show={show} onHide={onClose} centered size="lg">
      <Modal.Header closeButton>
        <Modal.Title>Přidat oprávnění do role: {roleName}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {error && <Alert variant="danger">{error}</Alert>}
          <Row className="g-3 mb-3">
            <Col sm={6}>
              <Form.Group controlId="perm-module">
                <Form.Label>Modul</Form.Label>
                <Form.Select
                  value={selectedModule}
                  onChange={e => {
                    setSelectedModule(e.target.value);
                    const mod = PERMISSION_CATALOG.find(m => m.code === e.target.value);
                    if (mod && mod.actions.length > 0) {
                      setSelectedAction(mod.actions[0].code);
                    }
                  }}
                >
                  {PERMISSION_CATALOG.map(m => (
                    <option key={m.code} value={m.code}>
                      {m.icon} {m.label} ({m.code})
                    </option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col sm={6}>
              <Form.Group controlId="perm-action">
                <Form.Label>Akce</Form.Label>
                <Form.Select
                  value={selectedAction}
                  onChange={e => setSelectedAction(e.target.value)}
                >
                  {activeModule?.actions.map(a => (
                    <option key={a.code} value={a.code}>
                      {a.label} ({a.code})
                    </option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col sm={6}>
              <Form.Group controlId="perm-scope-type">
                <Form.Label>Typ rozsahu (Scope)</Form.Label>
                <Form.Select
                  value={scopeType}
                  onChange={e => {
                    setScopeType(e.target.value);
                    if (e.target.value === 'GLOBAL') setScopeValue('*');
                  }}
                >
                  <option value="GLOBAL">Globální (všechny organizace)</option>
                  <option value="ORGANIZATION">Konkrétní organizace</option>
                </Form.Select>
              </Form.Group>
            </Col>
            {scopeType !== 'GLOBAL' && (
              <Col sm={6}>
                <Form.Group controlId="perm-scope-value">
                  <Form.Label>Hodnota rozsahu (Kód organizace)</Form.Label>
                  <Form.Control
                    required
                    placeholder="např. GJN-PRAHA"
                    value={scopeValue === '*' ? '' : scopeValue}
                    onChange={e => setScopeValue(e.target.value)}
                  />
                </Form.Group>
              </Col>
            )}
          </Row>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="outline-secondary" onClick={onClose} disabled={loading}>
            Zrušit
          </Button>
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? 'Přidávám...' : 'Přidat oprávnění'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}
