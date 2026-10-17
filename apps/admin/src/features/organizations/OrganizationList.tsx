'use client';

import { Badge, Button } from 'react-bootstrap';
import { TableCustom, type Column } from '@/shared/components/TableCustom';
import { StatusBadge } from '@/shared/components/StatusBadge';
import type { ApiOrganization } from './api';

export function OrganizationList({ organizations, onEdit, onSelect }: {
  organizations: ApiOrganization[];
  onEdit: (organization: ApiOrganization) => void;
  onSelect: (id: string) => void;
}) {
  const columns: Column<ApiOrganization>[] = [
    {
      header: 'Organizace',
      accessor: org => (
        <>
          <div className="fw-semibold d-flex align-items-center gap-2">
            {org.name}
            {org.isDefaultDispatch && (
              <Badge bg="warning" text="dark" className="fw-normal">
                Default dispečink
              </Badge>
            )}
          </div>
          <code>{org.code}</code>
        </>
      ),
    },
    {
      header: 'Kontaktní spojení',
      accessor: org => (
        <div>
          {org.contactEmail && <div className="text-break small">{org.contactEmail}</div>}
          {org.contactPhone && <div className="text-muted small">{org.contactPhone}</div>}
        </div>
      ),
    },
    {
      header: 'Areály & Geofence',
      accessor: org => (
        <div>
          {org.areas.length > 0 ? (
            <div>
              <span className="badge bg-light text-dark border">
                {org.areas.length} {org.areas.length === 1 ? 'areál' : 'areály'}
              </span>
              <div className="small text-muted mt-1">
                {org.areas[0].name} ({org.areas[0].offsetMeters}m offset)
              </div>
            </div>
          ) : (
            <span className="text-muted small">Bez polygonu</span>
          )}
        </div>
      ),
    },
    {
      header: 'Stav',
      accessor: org => (
        <StatusBadge
          status={org.status.toUpperCase() === 'ACTIVE' ? 'ACTIVE' : org.status.toUpperCase() === 'PLANNED' ? 'PLANNED' : 'INACTIVE'}
          preset="provider"
        />
      ),
    },
    {
      header: 'Akce',
      className: 'text-end',
      accessor: org => (
        <div className="d-flex flex-wrap gap-2 justify-content-end">
          <Button
            size="sm"
            variant="outline-secondary"
            aria-label={`Upravit organizaci ${org.name}`}
            onClick={() => onEdit(org)}
          >
            Upravit
          </Button>
          <Button
            size="sm"
            variant="outline-primary"
            onClick={() => onSelect(org.id)}
            aria-label={`Otevřít organizaci ${org.name}`}
          >
            Přepnout na velín
          </Button>
        </div>
      ),
    },
  ];

  return <TableCustom data={organizations} columns={columns} keyField="id" caption="Organizace Krizovky" />;
}
