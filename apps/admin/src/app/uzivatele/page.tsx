'use client';

import { useEffect, useState, useCallback } from 'react';
import { Alert, Button, Form, Spinner } from 'react-bootstrap';
import { ModulePage } from '@/shared/components/ModulePage';
import { AdminList } from '@/features/access-control/components/AdminList';
import { AdminViewModal, AdminFormModal, ResetPasswordModal } from '@/features/access-control/components/AdminModals';
import { accessControlApi } from '@/features/access-control/api';
import { BackofficeUser, BackofficeRole, CreateBackofficeUserRequest, UpdateBackofficeUserRequest } from '@/features/access-control/types';

export default function UsersPage() {
  const [users, setUsers] = useState<BackofficeUser[]>([]);
  const [roles, setRoles] = useState<BackofficeRole[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const [search, setSearch] = useState('');
  const [selectedRole, setSelectedRole] = useState('');

  const [viewUser, setViewUser] = useState<BackofficeUser | null>(null);
  const [editingUser, setEditingUser] = useState<BackofficeUser | 'new' | null>(null);
  const [resettingUser, setResettingUser] = useState<BackofficeUser | null>(null);

  const loadData = useCallback(() => {
    setLoading(true);
    setError('');
    Promise.all([
      accessControlApi.getUsers(),
      accessControlApi.getRoles().catch(() => []),
    ])
      .then(([fetchedUsers, fetchedRoles]) => {
        setUsers(fetchedUsers);
        setRoles(fetchedRoles);
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Nepodařilo se načíst uživatele ze služby US.');
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    let ignore = false;
    Promise.all([
      accessControlApi.getUsers(),
      accessControlApi.getRoles().catch(() => []),
    ])
      .then(([fetchedUsers, fetchedRoles]) => {
        if (ignore) return;
        setUsers(fetchedUsers);
        setRoles(fetchedRoles);
      })
      .catch((err: unknown) => {
        if (ignore) return;
        setError(err instanceof Error ? err.message : 'Nepodařilo se načíst uživatele ze služby US.');
      })
      .finally(() => {
        if (ignore) return;
        setLoading(false);
      });
    return () => { ignore = true; };
  }, []);

  const handleSaveUser = async (data: CreateBackofficeUserRequest | UpdateBackofficeUserRequest) => {
    if ('id' in data) {
      await accessControlApi.updateUser(data);
      setNotice(`Uživatel ${data.firstName} ${data.lastName} byl úspěšně upraven.`);
    } else {
      await accessControlApi.createUser(data);
      setNotice(`Uživatel ${data.firstName} ${data.lastName} byl vytvořen.`);
    }
    await loadData();
  };

  const handleDeleteUser = async (user: BackofficeUser) => {
    try {
      await accessControlApi.deleteUser(user.id);
      setNotice(`Uživatel ${user.firstName} ${user.lastName} byl smazán.`);
      await loadData();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při mazání uživatele.');
    }
  };

  const handleResetPassword = async (id: string, newPass: string) => {
    await accessControlApi.resetPassword(id, newPass);
    setNotice('Heslo bylo úspěšně změněno.');
  };

  const filteredUsers = users.filter(u => {
    const fullName = `${u.firstName || ''} ${u.lastName || ''} ${u.email}`.toLowerCase();
    const matchesSearch = !search.trim() || fullName.includes(search.toLowerCase().trim());
    const matchesRole = !selectedRole || u.roleId === selectedRole || u.roleCode === selectedRole;
    return matchesSearch && matchesRole;
  });

  return (
    <>
      <ModulePage
        title="Uživatelé a správa přístupů"
        subtitle="Administrátoři a operátoři velínu ze služby US"
        noCard
        actions={
          <div className="d-flex gap-2">
            <Button variant="outline-secondary" size="sm" onClick={loadData} disabled={loading}>
              {loading ? <Spinner animation="border" size="sm" /> : 'Obnovit'}
            </Button>
            <Button onClick={() => { setNotice(''); setEditingUser('new'); }}>
              Přidat uživatele
            </Button>
          </div>
        }
        filters={
          <div className="row g-3">
            <Form.Group controlId="user-search" className="col-md-6">
              <Form.Label>Hledat uživatele</Form.Label>
              <Form.Control
                type="search"
                placeholder="Jméno nebo e-mail"
                value={search}
                onChange={e => setSearch(e.target.value)}
              />
            </Form.Group>
            <Form.Group controlId="user-role-filter" className="col-md-6">
              <Form.Label>Filtrovat podle role</Form.Label>
              <Form.Select value={selectedRole} onChange={e => setSelectedRole(e.target.value)}>
                <option value="">Všechny role</option>
                {roles.map(r => (
                  <option key={r.id} value={r.id}>
                    {r.name} ({r.code})
                  </option>
                ))}
              </Form.Select>
            </Form.Group>
          </div>
        }
      >
        {notice && <Alert variant="success" dismissible onClose={() => setNotice('')}>{notice}</Alert>}
        {error && <Alert variant="danger">{error}</Alert>}

        {loading ? (
          <div className="text-center py-5">
            <Spinner animation="border" variant="primary" />
            <div className="small text-muted mt-2">Načítám uživatele z backendu...</div>
          </div>
        ) : (
          <>
            <p className="small text-secondary">
              Zobrazeno {filteredUsers.length} z {users.length} uživatelů
            </p>
            <AdminList
              admins={filteredUsers}
              onView={u => setViewUser(u)}
              onEdit={u => { setNotice(''); setEditingUser(u); }}
              onDelete={handleDeleteUser}
              onResetPassword={u => setResettingUser(u)}
            />
          </>
        )}
      </ModulePage>

      {/* VIEW MODAL */}
      {viewUser && (
        <AdminViewModal
          show={!!viewUser}
          admin={viewUser}
          onClose={() => setViewUser(null)}
          onEdit={u => { setViewUser(null); setEditingUser(u); }}
          onResetPassword={u => { setViewUser(null); setResettingUser(u); }}
        />
      )}

      {/* CREATE / EDIT MODAL */}
      {editingUser && (
        <AdminFormModal
          show={!!editingUser}
          admin={editingUser === 'new' ? null : editingUser}
          roles={roles}
          onClose={() => setEditingUser(null)}
          onSubmit={handleSaveUser}
        />
      )}

      {/* RESET PASSWORD MODAL */}
      {resettingUser && (
        <ResetPasswordModal
          show={!!resettingUser}
          admin={resettingUser}
          onClose={() => setResettingUser(null)}
          onSubmit={handleResetPassword}
        />
      )}
    </>
  );
}
