'use client';

import { useEffect, useState, useCallback } from 'react';
import { Alert, Button, Spinner } from 'react-bootstrap';
import { ModulePage } from '@/shared/components/ModulePage';
import { RoleList } from '@/features/access-control/components/RoleList';
import { CreateRoleModal, AddPermissionModal } from '@/features/access-control/components/RoleModals';
import { accessControlApi } from '@/features/access-control/api';
import { BackofficeRole, CreateRoleRequest, AddPermissionRequest } from '@/features/access-control/types';

export default function RolesPage() {
  const [roles, setRoles] = useState<BackofficeRole[]>([]);
  const [selectedRole, setSelectedRole] = useState<BackofficeRole | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const [createRoleOpen, setCreateRoleOpen] = useState(false);
  const [addPermOpen, setAddPermOpen] = useState(false);

  const loadRoles = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const fetched = await accessControlApi.getRoles();
      setRoles(fetched);
      if (selectedRole) {
        const refreshedSelected = fetched.find(r => r.id === selectedRole.id) || null;
        setSelectedRole(refreshedSelected);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Nepodařilo se načíst role ze služby US.');
    } finally {
      setLoading(false);
    }
  }, [selectedRole]);

  useEffect(() => {
    let ignore = false;
    accessControlApi.getRoles()
      .then(fetched => {
        if (ignore) return;
        setRoles(fetched);
      })
      .catch((err: unknown) => {
        if (ignore) return;
        setError(err instanceof Error ? err.message : 'Nepodařilo se načíst role ze služby US.');
      })
      .finally(() => {
        if (ignore) return;
        setLoading(false);
      });
    return () => { ignore = true; };
  }, []);

  const handleCreateRole = async (data: CreateRoleRequest) => {
    await accessControlApi.createRole(data);
    setNotice(`Role ${data.name} byla vytvořena.`);
    await loadRoles();
  };

  const handleDeleteRole = async (role: BackofficeRole) => {
    try {
      await accessControlApi.deleteRole(role.id);
      setNotice(`Role ${role.name} byla smazána.`);
      if (selectedRole?.id === role.id) setSelectedRole(null);
      await loadRoles();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při mazání role.');
    }
  };

  const handleAddPermission = async (perm: AddPermissionRequest) => {
    if (!selectedRole) return;
    await accessControlApi.addPermission(selectedRole.id, perm);
    setNotice('Oprávnění bylo přidáno.');
    await loadRoles();
  };

  const handleRemovePermission = async (roleId: string, permId: string) => {
    try {
      await accessControlApi.removePermission(permId);
      setNotice('Oprávnění bylo odebráno.');
      await loadRoles();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Chyba při odebírání oprávnění.');
    }
  };

  return (
    <>
      <ModulePage
        title="Role a oprávnění"
        subtitle="Správa přístupových rolí a katalogu oprávnění velínu"
        noCard
        actions={
          <div className="d-flex gap-2">
            <Button variant="outline-secondary" size="sm" onClick={loadRoles} disabled={loading}>
              {loading ? <Spinner animation="border" size="sm" /> : 'Obnovit'}
            </Button>
            <Button onClick={() => { setNotice(''); setCreateRoleOpen(true); }}>
              Vytvořit roli
            </Button>
          </div>
        }
      >
        {notice && <Alert variant="success" dismissible onClose={() => setNotice('')}>{notice}</Alert>}
        {error && <Alert variant="danger">{error}</Alert>}

        {loading && roles.length === 0 ? (
          <div className="text-center py-5">
            <Spinner animation="border" variant="primary" />
            <div className="small text-muted mt-2">Načítám role a oprávnění...</div>
          </div>
        ) : (
          <RoleList
            roles={roles}
            selectedRole={selectedRole}
            onSelectRole={role => setSelectedRole(role)}
            onRemovePermission={handleRemovePermission}
            onOpenAddPermission={() => setAddPermOpen(true)}
            onDeleteRole={handleDeleteRole}
            onCreateRole={() => setCreateRoleOpen(true)}
          />
        )}
      </ModulePage>

      {createRoleOpen && (
        <CreateRoleModal
          show={createRoleOpen}
          onClose={() => setCreateRoleOpen(false)}
          onCreate={handleCreateRole}
        />
      )}

      {addPermOpen && (
        <AddPermissionModal
          show={addPermOpen}
          roleName={selectedRole?.name}
          onClose={() => setAddPermOpen(false)}
          onAdd={handleAddPermission}
        />
      )}
    </>
  );
}
