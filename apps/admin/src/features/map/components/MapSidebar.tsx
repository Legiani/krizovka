'use client';

import React, { useState } from 'react';
import { Form, Badge, Button } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faLayerGroup,
  faExclamationTriangle,
  faChevronLeft,
  faChevronRight,
  faFilter,
} from '@fortawesome/free-solid-svg-icons';
import type { ReportStatus } from '@/demo/types';
import type { DisplayReport } from '../types';

interface MapSidebarProps {
  showReports: boolean;
  onToggleReports: (show: boolean) => void;
  selectedStatuses: ReportStatus[];
  onSelectedStatusesChange: (statuses: ReportStatus[]) => void;
  reports: DisplayReport[];
}

interface StatusOption {
  value: ReportStatus;
  label: string;
  color: string;
}

const STATUS_OPTIONS: StatusOption[] = [
  { value: 'New', label: 'Nové / Neřešeno', color: '#dc3545' },
  { value: 'Accepted', label: 'Převzaté / V řešení', color: '#ffc107' },
  { value: 'Closed', label: 'Uzavřené', color: '#198754' },
];

export const MapSidebar: React.FC<MapSidebarProps> = ({
  showReports,
  onToggleReports,
  selectedStatuses,
  onSelectedStatusesChange,
  reports,
}) => {
  const [isCollapsed, setIsCollapsed] = useState(false);

  const toggleStatus = (status: ReportStatus) => {
    if (selectedStatuses.includes(status)) {
      onSelectedStatusesChange(selectedStatuses.filter(s => s !== status));
    } else {
      onSelectedStatusesChange([...selectedStatuses, status]);
    }
  };

  const selectAll = () => {
    onSelectedStatusesChange([]);
  };

  const localizedCount = reports.filter(
    r =>
      r.latitude !== undefined &&
      r.latitude !== null &&
      r.longitude !== undefined &&
      r.longitude !== null
  ).length;

  if (isCollapsed) {
    return (
      <div
        className="position-absolute z-3 m-3"
        style={{ top: 0, left: 0 }}
      >
        <Button
          variant="light"
          size="sm"
          className="shadow-sm border rounded-circle d-flex align-items-center justify-content-center"
          style={{ width: 40, height: 40 }}
          onClick={() => setIsCollapsed(false)}
          title="Otevřít vrstvy a filtry"
          aria-label="Otevřít vrstvy a filtry"
        >
          <FontAwesomeIcon icon={faChevronRight} />
        </Button>
      </div>
    );
  }

  return (
    <div
      className="bg-white shadow-sm border rounded m-3 p-3 position-absolute z-3"
      style={{
        top: 0,
        left: 0,
        width: 310,
        maxHeight: 'calc(100% - 30px)',
        overflowY: 'auto',
        opacity: 0.96,
        backdropFilter: 'blur(4px)',
      }}
    >
      <div className="d-flex align-items-center justify-content-between mb-3 border-bottom pb-2">
        <div className="d-flex align-items-center gap-2">
          <FontAwesomeIcon icon={faLayerGroup} className="text-primary fs-5" />
          <h6 className="mb-0 fw-bold">Mapové vrstvy</h6>
        </div>
        <Button
          variant="link"
          size="sm"
          className="text-secondary p-0 text-decoration-none"
          onClick={() => setIsCollapsed(true)}
          title="Sbalit panel"
          aria-label="Sbalit panel"
        >
          <FontAwesomeIcon icon={faChevronLeft} />
        </Button>
      </div>

      <div className="border rounded p-3 bg-light mb-3">
        <div className="d-flex align-items-center justify-content-between">
          <div className="d-flex align-items-center gap-2">
            <FontAwesomeIcon icon={faExclamationTriangle} className="text-danger" />
            <span className="fw-semibold small">Krizová hlášení</span>
          </div>
          <Form.Check
            type="switch"
            id="toggle-reports-layer"
            checked={showReports}
            onChange={e => onToggleReports(e.target.checked)}
            title="Zapnout/vypnout vrstvu"
          />
        </div>

        <div className="mt-2 text-secondary" style={{ fontSize: '0.78rem' }}>
          Lokalizováno {localizedCount} z {reports.length} incidentů
        </div>

        {showReports && (
          <div className="mt-3 pt-2 border-top">
            <div className="d-flex align-items-center justify-content-between mb-2">
              <span className="text-muted text-uppercase fw-bold" style={{ fontSize: '0.72rem', letterSpacing: '0.5px' }}>
                <FontAwesomeIcon icon={faFilter} className="me-1" />
                Stav incidentu
              </span>
              {selectedStatuses.length > 0 && (
                <button
                  type="button"
                  className="btn btn-link p-0 text-primary small text-decoration-none"
                  style={{ fontSize: '0.75rem' }}
                  onClick={selectAll}
                >
                  Vše
                </button>
              )}
            </div>

            <div className="d-flex flex-column gap-2">
              {STATUS_OPTIONS.map(opt => {
                const count = reports.filter(r => r.status === opt.value).length;
                const isChecked =
                  selectedStatuses.length === 0 || selectedStatuses.includes(opt.value);

                return (
                  <div
                    key={opt.value}
                    className="d-flex align-items-center justify-content-between py-1 px-2 rounded hover-bg"
                    style={{ cursor: 'pointer', userSelect: 'none' }}
                    onClick={() => toggleStatus(opt.value)}
                  >
                    <div className="d-flex align-items-center gap-2">
                      <Form.Check
                        type="checkbox"
                        id={`status-check-${opt.value}`}
                        className="mb-0"
                        checked={isChecked}
                        onChange={() => toggleStatus(opt.value)}
                        onClick={e => e.stopPropagation()}
                      />
                      <span
                        className="rounded-circle d-inline-block"
                        style={{
                          width: 10,
                          height: 10,
                          backgroundColor: opt.color,
                        }}
                      />
                      <span className="small text-dark">{opt.label}</span>
                    </div>
                    <Badge
                      bg={isChecked ? 'secondary' : 'light'}
                      text={isChecked ? undefined : 'secondary'}
                      className="rounded-pill"
                      style={{ fontSize: '0.72rem' }}
                    >
                      {count}
                    </Badge>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

      <div className="text-secondary small" style={{ fontSize: '0.75rem' }}>
        Tip: Kliknutím na shluk bodů mapu přiblížíte, kliknutím na jednotlivý bod otevřete detail incidentu.
      </div>
    </div>
  );
};
