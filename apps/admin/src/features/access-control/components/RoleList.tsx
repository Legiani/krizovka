'use client';

import React, { useState } from 'react';
import { Card, Badge, Button } from 'react-bootstrap';
import { TableCustom, Column } from '@/shared/components/TableCustom';
import { ConfirmModal } from '@/shared/components/ConfirmModal';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTrash, faPlus, faShieldAlt } from '@fortawesome/free-solid-svg-icons';
import { BackofficeRole, Permission } from '../types';
import { getModuleLabel, getActionLabel, isSystemRole } from '../permissionCatalog';

interface RoleListProps {
  roles: BackofficeRole[];
  selectedRole: BackofficeRole | null;
  onSelectRole: (role: BackofficeRole) => void;
  onRemovePermission: (roleId: string, permId: string) => void;
  onOpenAddPermission: () => void;
  onDeleteRole: (role: BackofficeRole) => void;
  onCreateRole: () => void;
}

export function RoleList({
  roles,
  selectedRole,
  onSelectRole,
  onRemovePermission,
  onOpenAddPermission,
  onDeleteRole,
  onCreateRole,
}: RoleListProps) {
  const [confirmDelete, setConfirmDelete] = useState<BackofficeRole | null>(null);

  const roleColumns: Column<BackofficeRole>[] = [
    {
      header: 'Role',
      accessor: role => (
        <div>
          <div className="fw-bold">{role.name}</div>
          <small className="text-muted"><code>{role.code}</code></small>
        </div>
      ),
    },
    {
      header: 'Počet oprávnění',
      accessor: role => (
        <Badge bg="secondary" pill>
          {role.permissions?.length || 0}
        </Badge>
      ),
    },
    {
      header: 'Typ',
      accessor: role => (
        isSystemRole(role.code) ? (
          <Badge bg="warning" text="dark">Systémová</Badge>
        ) : (
          <Badge bg="info">Vlastní</Badge>
        )
      ),
    },
    {
      header: 'Akce',
      className: 'text-end',
      accessor: role => (
        <div className="d-flex gap-2 justify-content-end">
          <Button
            variant={selectedRole?.id === role.id ? 'primary' : 'outline-primary'}
            size="sm"
            onClick={() => onSelectRole(role)}
          >
            Oprávnění
          </Button>
          {!isSystemRole(role.code) && (
            <Button
              variant="outline-danger"
              size="sm"
              onClick={() => setConfirmDelete(role)}
              title="Smazat roli"
            >
              <FontAwesomeIcon icon={faTrash} />
            </Button>
          )}
        </div>
      ),
    },
  ];

  const permColumns: Column<Permission>[] = [
    {
      header: 'Modul',
      accessor: perm => <strong>{getModuleLabel(perm.module)}</strong>,
    },
    {
      header: 'Akce',
      accessor: perm => <Badge bg="primary">{getActionLabel(perm.module, perm.action)}</Badge>,
    },
    {
      header: 'Rozsah (Scope)',
      accessor: perm => (
        <span>
          {perm.scopeType === 'GLOBAL' ? '🌍 Globální' : `🏢 ${perm.scopeValue || perm.scopeType}`}
        </span>
      ),
    },
    {
      header: '',
      className: 'text-end',
      accessor: perm => (
        <Button
          variant="outline-danger"
          size="sm"
          onClick={() => selectedRole && onRemovePermission(selectedRole.id, perm.id)}
          title="Odebrat oprávnění"
        >
          <FontAwesomeIcon icon={faTrash} />
        </Button>
      ),
    },
  ];

  return (
    <div className="row g-4">
      <div className="col-lg-5">
        <Card className="shadow-sm border-0">
          <Card.Header className="bg-white d-flex justify-content-between align-items-center py-3">
            <h5 className="mb-0 fs-6 fw-bold">Seznam rolí</h5>
            <Button variant="primary" size="sm" onClick={onCreateRole}>
              <FontAwesomeIcon icon={faPlus} className="me-1" />
              Nová role
            </Button>
          </Card.Header>
          <Card.Body className="p-0">
            <TableCustom data={roles} columns={roleColumns} keyField="id" caption="Systémové a uživatelské role" />
          </Card.Body>
        </Card>
      </div>

      <div className="col-lg-7">
        {selectedRole ? (
          <Card className="shadow-sm border-0">
            <Card.Header className="bg-white d-flex justify-content-between align-items-center py-3">
              <div>
                <h5 className="mb-0 fs-6 fw-bold">
                  <FontAwesomeIcon icon={faShieldAlt} className="text-primary me-2" />
                  Oprávnění role: {selectedRole.name}
                </h5>
                <small className="text-muted">{selectedRole.description || selectedRole.code}</small>
              </div>
              <Button variant="outline-primary" size="sm" onClick={onOpenAddPermission}>
                <FontAwesomeIcon icon={faPlus} className="me-1" />
                Přidat oprávnění
              </Button>
            </Card.Header>
            <Card.Body className="p-0">
              <TableCustom
                data={selectedRole.permissions || []}
                columns={permColumns}
                keyField="id"
                caption={`Oprávnění role ${selectedRole.name}`}
              />
            </Card.Body>
          </Card>
        ) : (
          <Card className="shadow-sm border-0 p-5 text-center text-muted">
            <div className="py-4">
              <FontAwesomeIcon icon={faShieldAlt} size="3x" className="mb-3 text-secondary opacity-50" />
              <h6>Vyberte roli ze seznamu</h6>
              <p className="small mb-0">Kliknutím na tlačítko &quot;Oprávnění&quot; zobrazíte a upravíte práva vybrané role.</p>
            </div>
          </Card>
        )}
      </div>

      {confirmDelete && (
        <ConfirmModal
          show={!!confirmDelete}
          title="Smazat roli"
          message={`Opravdu si přejete smazat roli ${confirmDelete.name} (${confirmDelete.code})? Tato akce je nevratná.`}
          confirmLabel="Smazat roli"
          confirmVariant="danger"
          onHide={() => setConfirmDelete(null)}
          onConfirm={() => {
            onDeleteRole(confirmDelete);
            setConfirmDelete(null);
          }}
        />
      )}
    </div>
  );
}
