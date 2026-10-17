'use client';

import React, { useState } from 'react';
import { Button, Badge } from 'react-bootstrap';
import { BackofficeUser } from '../types';
import { TableCustom, Column } from '@/shared/components/TableCustom';
import { ConfirmModal } from '@/shared/components/ConfirmModal';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCrown, faEye, faEdit, faTrash, faKey } from '@fortawesome/free-solid-svg-icons';

interface AdminListProps {
  admins: BackofficeUser[];
  currentUserId?: string;
  onView: (admin: BackofficeUser) => void;
  onEdit: (admin: BackofficeUser) => void;
  onDelete: (admin: BackofficeUser) => void;
  onResetPassword: (admin: BackofficeUser) => void;
}

export function AdminList({ admins, currentUserId, onView, onEdit, onDelete, onResetPassword }: AdminListProps) {
  const [confirmDelete, setConfirmDelete] = useState<BackofficeUser | null>(null);

  const columns: Column<BackofficeUser>[] = [
    {
      header: 'Jméno',
      accessor: admin => (
        <>
          <div className="fw-bold">
            {admin.firstName || admin.lastName ? `${admin.firstName} ${admin.lastName}` : admin.email}
            {admin.isSuperAdmin && (
              <FontAwesomeIcon icon={faCrown} className="ms-1 text-danger" style={{ fontSize: '0.75rem' }} title="Super Admin" />
            )}
          </div>
          <small className="text-muted"><code>{admin.id.substring(0, 8)}...</code></small>
        </>
      ),
    },
    { header: 'E-mail', accessor: 'email' },
    { header: 'Role', accessor: admin => admin.roleName || admin.roleCode || '–' },
    {
      header: 'Super Admin',
      accessor: admin => (
        admin.isSuperAdmin ? <Badge bg="danger">Ano</Badge> : <Badge bg="secondary">Ne</Badge>
      ),
    },
    {
      header: 'Akce',
      className: 'text-end',
      accessor: admin => (
        <div className="d-flex gap-2 justify-content-end">
          <Button
            variant="outline-primary"
            size="sm"
            onClick={() => onView(admin)}
            title="Detail a oprávnění"
          >
            <FontAwesomeIcon icon={faEye} className="me-1" />
            Detail
          </Button>
          <Button
            variant="outline-secondary"
            size="sm"
            onClick={() => onResetPassword(admin)}
            title="Změnit heslo"
          >
            <FontAwesomeIcon icon={faKey} />
          </Button>
          <Button
            variant="outline-secondary"
            size="sm"
            onClick={() => onEdit(admin)}
            title="Upravit"
          >
            <FontAwesomeIcon icon={faEdit} />
          </Button>
          {admin.id !== currentUserId && (
            <Button
              variant="outline-danger"
              size="sm"
              onClick={() => setConfirmDelete(admin)}
              title="Smazat uživatele"
            >
              <FontAwesomeIcon icon={faTrash} />
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <TableCustom data={admins} columns={columns} keyField="id" caption="Administrátoři a operátoři velínu" />
      {confirmDelete && (
        <ConfirmModal
          show={!!confirmDelete}
          title="Smazat uživatele"
          message={`Opravdu si přejete smazat administrátora ${confirmDelete.firstName} ${confirmDelete.lastName} (${confirmDelete.email})?`}
          confirmLabel="Smazat"
          confirmVariant="danger"
          onHide={() => setConfirmDelete(null)}
          onConfirm={() => {
            onDelete(confirmDelete);
            setConfirmDelete(null);
          }}
        />
      )}
    </>
  );
}
