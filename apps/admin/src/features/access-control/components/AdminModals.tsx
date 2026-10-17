'use client';

import React, { useState } from 'react';
import { Modal, Button, Form, Row, Col, Alert, Badge, Tabs, Tab } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUserShield, faKey } from '@fortawesome/free-solid-svg-icons';
import { BackofficeUser, BackofficeRole, CreateBackofficeUserRequest, UpdateBackofficeUserRequest } from '../types';

// VIEW MODAL
export function AdminViewModal({
  show,
  admin,
  onClose,
  onEdit,
  onResetPassword,
}: {
  show: boolean;
  admin: BackofficeUser | null;
  onClose: () => void;
  onEdit?: (admin: BackofficeUser) => void;
  onResetPassword?: (admin: BackofficeUser) => void;
}) {
  if (!admin) return null;

  return (
    <Modal show={show} onHide={onClose} size="lg" centered>
      <Modal.Header closeButton>
        <div className="d-flex align-items-center gap-2">
          <FontAwesomeIcon icon={faUserShield} className="text-primary" />
          <Modal.Title style={{ fontSize: '1.1rem' }}>
            {admin.firstName} {admin.lastName}
            {admin.isSuperAdmin && (
              <Badge bg="danger" className="ms-2">Super Admin</Badge>
            )}
            {admin.roleName && (
              <Badge bg="secondary" className="ms-1">{admin.roleName}</Badge>
            )}
          </Modal.Title>
        </div>
      </Modal.Header>
      <Modal.Body>
        <Tabs defaultActiveKey="info" className="mb-3">
          <Tab eventKey="info" title="Základní informace">
            <Row className="g-3">
              <Col sm={6}>
                <div className="small text-muted text-uppercase fw-bold">E-mail</div>
                <div className="fs-6">{admin.email}</div>
              </Col>
              <Col sm={6}>
                <div className="small text-muted text-uppercase fw-bold">ID uživatele</div>
                <div className="fs-6 text-truncate"><code>{admin.id}</code></div>
              </Col>
              <Col sm={6}>
                <div className="small text-muted text-uppercase fw-bold">Přiřazená role</div>
                <div className="fs-6">{admin.roleName || admin.roleCode || 'Žádná role'}</div>
              </Col>
              <Col sm={6}>
                <div className="small text-muted text-uppercase fw-bold">Typ účtu</div>
                <div className="fs-6">{admin.isSuperAdmin ? 'Globální Super Administrátor' : 'Standardní operátor'}</div>
              </Col>
            </Row>
          </Tab>
          <Tab eventKey="permissions" title={`Oprávnění (${admin.permissions?.length || 0})`}>
            {admin.permissions && admin.permissions.length > 0 ? (
              <div className="d-flex flex-wrap gap-2">
                {admin.permissions.map((p, idx) => (
                  <Badge bg="light" text="dark" className="border px-2 py-1" key={idx}>
                    {p}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="text-muted small mb-0">Uživatel dědí oprávnění podle své role nebo nemá žádná specifická oprávnění.</p>
            )}
          </Tab>
        </Tabs>
      </Modal.Body>
      <Modal.Footer>
        {onResetPassword && (
          <Button variant="outline-warning" size="sm" onClick={() => onResetPassword(admin)}>
            <FontAwesomeIcon icon={faKey} className="me-1" />
            Reset hesla
          </Button>
        )}
        {onEdit && (
          <Button variant="outline-primary" size="sm" onClick={() => onEdit(admin)}>
            Upravit
          </Button>
        )}
        <Button variant="secondary" size="sm" onClick={onClose}>
          Zavřít
        </Button>
      </Modal.Footer>
    </Modal>
  );
}

// CREATE / EDIT FORM MODAL
export function AdminFormModal({
  show,
  admin,
  roles,
  onClose,
  onSubmit,
}: {
  show: boolean;
  admin: BackofficeUser | null;
  roles: BackofficeRole[];
  onClose: () => void;
  onSubmit: (data: CreateBackofficeUserRequest | UpdateBackofficeUserRequest) => Promise<void>;
}) {
  const isEditing = !!admin;
  const [email, setEmail] = useState(admin?.email || '');
  const [firstName, setFirstName] = useState(admin?.firstName || '');
  const [lastName, setLastName] = useState(admin?.lastName || '');
  const [roleId, setRoleId] = useState<string>(admin?.roleId || '');
  const [isSuperAdmin, setIsSuperAdmin] = useState(admin?.isSuperAdmin || false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim() || !firstName.trim() || !lastName.trim()) {
      setError('Vyplňte prosím e-mail, jméno a příjmení.');
      return;
    }
    setError('');
    setLoading(true);
    try {
      if (isEditing) {
        await onSubmit({
          id: admin.id,
          email: email.trim().toLowerCase(),
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          roleId: roleId || null,
          isSuperAdmin,
        });
      } else {
        await onSubmit({
          email: email.trim().toLowerCase(),
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          roleId: roleId || null,
          isSuperAdmin,
        });
      }
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při ukládání uživatele.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal show={show} onHide={onClose} centered size="lg">
      <Modal.Header closeButton>
        <Modal.Title>{isEditing ? 'Upravit uživatele' : 'Přidat administrátora / operátora'}</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          {error && <Alert variant="danger">{error}</Alert>}
          <Row className="g-3 mb-3">
            <Col sm={6}>
              <Form.Group controlId="admin-first-name">
                <Form.Label>Jméno</Form.Label>
                <Form.Control
                  required
                  autoFocus
                  value={firstName}
                  onChange={e => setFirstName(e.target.value)}
                  placeholder="např. Jan"
                />
              </Form.Group>
            </Col>
            <Col sm={6}>
              <Form.Group controlId="admin-last-name">
                <Form.Label>Příjmení</Form.Label>
                <Form.Control
                  required
                  value={lastName}
                  onChange={e => setLastName(e.target.value)}
                  placeholder="např. Novák"
                />
              </Form.Group>
            </Col>
            <Col sm={12}>
              <Form.Group controlId="admin-email">
                <Form.Label>Pracovní E-mail</Form.Label>
                <Form.Control
                  required
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="operator@organizace.cz"
                />
              </Form.Group>
            </Col>
            <Col sm={12}>
              <Form.Group controlId="admin-role">
                <Form.Label>Přiřazená role</Form.Label>
                <Form.Select value={roleId} onChange={e => setRoleId(e.target.value)}>
                  <option value="">– Bez specifické role –</option>
                  {roles.map(r => (
                    <option key={r.id} value={r.id}>
                      {r.name} ({r.code})
                    </option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>
            <Col sm={12}>
              <Form.Check
                type="checkbox"
                id="admin-superadmin"
                label="Super Administrátor (neomezený přístup k celému systému a všem organizacím)"
                checked={isSuperAdmin}
                onChange={e => setIsSuperAdmin(e.target.checked)}
              />
            </Col>
          </Row>
          {!isEditing && (
            <p className="small text-muted mb-0">
              Nově vytvořený účet dostane počáteční dočasné heslo <code>Krizovka123!</code>.
            </p>
          )}
        </Modal.Body>
        <Modal.Footer>
          <Button variant="outline-secondary" onClick={onClose} disabled={loading}>
            Zrušit
          </Button>
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? 'Ukládám...' : isEditing ? 'Uložit změny' : 'Vytvořit uživatele'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}

// RESET PASSWORD MODAL
export function ResetPasswordModal({
  show,
  admin,
  onClose,
  onSubmit,
}: {
  show: boolean;
  admin: BackofficeUser | null;
  onClose: () => void;
  onSubmit: (id: string, newPassword: string) => Promise<void>;
}) {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (password.length < 8) {
      setError('Heslo musí mít alespoň 8 znaků.');
      return;
    }
    if (password !== confirmPassword) {
      setError('Hesla se neshodují.');
      return;
    }
    setError('');
    setLoading(true);
    try {
      if (admin) {
        await onSubmit(admin.id, password);
      }
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při změně hesla.');
    } finally {
      setLoading(false);
    }
  };

  if (!admin) return null;

  return (
    <Modal show={show} onHide={onClose} centered>
      <Modal.Header closeButton>
        <Modal.Title>Změnit heslo uživatele</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          <p className="small text-muted mb-3">
            Uživatel: <strong>{admin.firstName} {admin.lastName}</strong> ({admin.email})
          </p>
          {error && <Alert variant="danger">{error}</Alert>}
          <Form.Group className="mb-3" controlId="new-password">
            <Form.Label>Nové heslo</Form.Label>
            <Form.Control
              type="password"
              required
              minLength={8}
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Minimálně 8 znaků"
            />
          </Form.Group>
          <Form.Group className="mb-3" controlId="confirm-password">
            <Form.Label>Potvrdit nové heslo</Form.Label>
            <Form.Control
              type="password"
              required
              minLength={8}
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
            />
          </Form.Group>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="outline-secondary" onClick={onClose} disabled={loading}>
            Zrušit
          </Button>
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? 'Měním heslo...' : 'Nastavit heslo'}
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
}
