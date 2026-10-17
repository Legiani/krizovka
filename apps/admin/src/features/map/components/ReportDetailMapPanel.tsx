'use client';

import React from 'react';
import { Button, Badge, Card } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faTimes,
  faMapMarkerAlt,
  faClock,
  faExternalLinkAlt,
  faUser,
  faPhone,
  faFolderOpen,
  faBullhorn,
} from '@fortawesome/free-solid-svg-icons';
import { reportStatusMap } from '@/demo/types';
import { StatusBadge } from '@/shared/components/StatusBadge';
import type { SelectedMapReport } from '../types';

interface ReportDetailMapPanelProps {
  report: SelectedMapReport;
  onClose: () => void;
  onOpenDetail: (reportId: string) => void;
}

const dateTime = (value: string) => {
  try {
    return new Intl.DateTimeFormat('cs', {
      dateStyle: 'short',
      timeStyle: 'short',
      timeZone: 'Europe/Prague',
    }).format(new Date(value));
  } catch {
    return value;
  }
};

export const ReportDetailMapPanel: React.FC<ReportDetailMapPanelProps> = ({
  report,
  onClose,
  onOpenDetail,
}) => {
  const mapUrl = `https://www.google.com/maps?q=${report.latitude},${report.longitude}`;

  return (
    <div
      className="bg-white shadow-lg border-start position-absolute end-0 top-0 bottom-0 z-3 d-flex flex-column"
      style={{
        width: 380,
        maxWidth: '100%',
        animation: 'slideInRight 0.25s ease-out',
      }}
    >
      {/* Header */}
      <div className="p-3 border-bottom d-flex align-items-center justify-content-between bg-light">
        <div className="d-flex align-items-center gap-2">
          <StatusBadge status={report.status} statusMap={reportStatusMap} />
          {report.isLive ? (
            <Badge bg="success" className="ms-1">
              Live
            </Badge>
          ) : (
            <Badge bg="secondary" className="ms-1">
              Demo
            </Badge>
          )}
        </div>
        <Button
          variant="light"
          size="sm"
          className="rounded-circle p-1 d-flex align-items-center justify-content-center"
          style={{ width: 28, height: 28 }}
          onClick={onClose}
          aria-label="Zavřít panel detailu"
        >
          <FontAwesomeIcon icon={faTimes} />
        </Button>
      </div>

      {/* Content */}
      <div className="p-3 flex-grow-1 overflow-auto">
        <div className="mb-3">
          <span className="text-secondary small text-uppercase fw-semibold" style={{ fontSize: '0.75rem', letterSpacing: '0.5px' }}>
            Identifikátor hlášení
          </span>
          <div className="fw-mono fw-bold fs-6 text-dark">{report.id}</div>
        </div>

        <div className="mb-3">
          <span className="text-secondary small text-uppercase fw-semibold" style={{ fontSize: '0.75rem', letterSpacing: '0.5px' }}>
            Obsah hlášení
          </span>
          <Card className="mt-1 bg-light border-0">
            <Card.Body className="p-3">
              <div className="fw-semibold text-dark fs-6">{report.title}</div>
              {report.description && report.description !== report.title && (
                <div className="small text-secondary mt-1">{report.description}</div>
              )}
            </Card.Body>
          </Card>
        </div>

        <div className="d-flex flex-column gap-3 py-2 border-top border-bottom my-3">
          <div className="d-flex align-items-start gap-2">
            <FontAwesomeIcon icon={faClock} className="text-secondary mt-1" />
            <div>
              <div className="text-secondary small" style={{ fontSize: '0.75rem' }}>Čas nahlášení</div>
              <div className="small fw-semibold">{dateTime(report.createdAt)}</div>
            </div>
          </div>

          <div className="d-flex align-items-start gap-2">
            <FontAwesomeIcon icon={faBullhorn} className="text-secondary mt-1" />
            <div>
              <div className="text-secondary small" style={{ fontSize: '0.75rem' }}>Kanál / Typ</div>
              <div className="small fw-semibold">{report.channel}</div>
            </div>
          </div>

          <div className="d-flex align-items-start gap-2">
            <FontAwesomeIcon icon={faMapMarkerAlt} className="text-danger mt-1" />
            <div className="flex-grow-1">
              <div className="text-secondary small" style={{ fontSize: '0.75rem' }}>Místo & GPS poloha</div>
              <div className="small fw-semibold">{report.place}</div>
              <div className="small text-secondary font-monospace mt-1">
                {report.latitude.toFixed(5)}, {report.longitude.toFixed(5)}
              </div>
              <a
                href={mapUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-outline-secondary btn-sm mt-1 py-0 px-2 text-decoration-none"
                style={{ fontSize: '0.72rem' }}
              >
                <FontAwesomeIcon icon={faExternalLinkAlt} className="me-1" />
                Otevřít v mapách
              </a>
            </div>
          </div>

          {report.reporterName && (
            <div className="d-flex align-items-start gap-2">
              <FontAwesomeIcon icon={faUser} className="text-secondary mt-1" />
              <div>
                <div className="text-secondary small" style={{ fontSize: '0.75rem' }}>Oznamovatel</div>
                <div className="small fw-semibold">{report.reporterName}</div>
                {report.reporterPhone && (
                  <div className="small text-secondary d-flex align-items-center gap-1 mt-1">
                    <FontAwesomeIcon icon={faPhone} />
                    <a href={`tel:${report.reporterPhone}`} className="text-decoration-none">
                      {report.reporterPhone}
                    </a>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Footer Action */}
      <div className="p-3 border-top bg-light">
        <Button
          variant="primary"
          className="w-100 d-flex align-items-center justify-content-center gap-2 fw-semibold"
          onClick={() => onOpenDetail(report.id)}
        >
          <FontAwesomeIcon icon={faFolderOpen} />
          Otevřít řešitelský detail
        </Button>
      </div>
    </div>
  );
};
