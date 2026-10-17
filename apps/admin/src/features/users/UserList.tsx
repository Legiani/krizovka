'use client';

// Adapted from City+ access-control/AdminList; membership is organization-specific.
import { Badge, Button } from 'react-bootstrap';
import { Column, TableCustom } from '@/shared/components/TableCustom';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { roleLabels, type DemoUser } from '@/demo/types';

export function UserList({ users, organizationId, onEdit }: {
  users: DemoUser[]; organizationId: string; onEdit: (user: DemoUser) => void;
}) {
  const columns: Column<DemoUser>[] = [
    { header: 'Uživatel', accessor: user => <><div className="fw-semibold">{user.firstName} {user.lastName}</div><span className="small text-secondary">{user.personnelNumber || 'Bez osobního čísla'}</span></> },
    { header: 'Kontakt', accessor: user => <><div className="text-break">{user.email}</div><span className="small text-secondary">{user.phone || 'Telefon neuveden'}</span></> },
    { header: 'Role v organizaci', accessor: user => <Badge bg="primary-subtle" text="primary-emphasis">{roleLabels[user.memberships.find(item => item.organizationId === organizationId)!.role]}</Badge> },
    { header: 'Stav', accessor: user => <StatusBadge status={user.isActive ? 'ACTIVE' : 'INACTIVE'} preset="user" /> },
    { header: 'Akce', className: 'text-end', accessor: user => <Button variant="outline-secondary" size="sm" aria-label={`Upravit uživatele ${user.firstName} ${user.lastName}`} onClick={() => onEdit(user)}>Upravit</Button> },
  ];
  return <TableCustom data={users} columns={columns} keyField="id" caption="Uživatelé aktivní organizace" />;
}
