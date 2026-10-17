'use client';

import { useEffect, useState, useCallback } from 'react';
import dynamic from 'next/dynamic';
import { Badge, Button, ButtonGroup, Modal, Spinner } from 'react-bootstrap';
import { useDemo, updateReport } from '@/demo/store';
import { reportStatusMap, type ReportStatus } from '@/demo/types';
import { ModulePage } from '@/shared/components/ModulePage';
import { TableCustom, type Column } from '@/shared/components/TableCustom';
import { StatusBadge } from '@/shared/components/StatusBadge';
import {
  fetchReports,
  fetchReportDetail,
  updateReportStatus,
  uploadReportAttachment,
  type ReportSummary,
  type ReportDetail,
} from '@/features/reports/api';
import { authApi } from '@/features/auth/api';
import type { CurrentUser } from '@/features/auth/types';
import type { DisplayReport } from '@/features/map/types';

const IncidentMapModule = dynamic(
  () => import('@/features/map/IncidentMapModule'),
  {
    ssr: false,
    loading: () => (
      <div
        className="d-flex flex-column align-items-center justify-content-center bg-light border rounded-3 p-5"
        style={{ minHeight: 480 }}
      >
        <Spinner animation="border" variant="primary" className="mb-2" />
        <span className="text-secondary small">Načítám interaktivní mapu incidentů...</span>
      </div>
    ),
  }
);

const dateTime = (value: string) =>
  new Intl.DateTimeFormat('cs', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'Europe/Prague',
  }).format(new Date(value));

export default function ReportsPage() {
  const { state, activeOrganization } = useDemo();
  const [viewMode, setViewMode] = useState<'table' | 'map' | 'split'>('table');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedDetail, setSelectedDetail] = useState<ReportDetail | null>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [status, setStatus] = useState('');
  const [notice, setNotice] = useState('');
  const [errorNotice, setErrorNotice] = useState('');
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
  const [uploadingAttachment, setUploadingAttachment] = useState(false);
  const [attachmentError, setAttachmentError] = useState('');
  const [listeningToStream, setListeningToStream] = useState(false);

  const formatBytes = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const handleUploadAttachment = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !selectedId) return;

    setUploadingAttachment(true);
    setAttachmentError('');
    try {
      const newAtt = await uploadReportAttachment(selectedId, file);
      setSelectedDetail(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          attachments: [...(prev.attachments || []), newAtt],
        };
      });
      e.target.value = '';
    } catch (err) {
      setAttachmentError(err instanceof Error ? err.message : 'Nahrávání selhalo.');
    } finally {
      setUploadingAttachment(false);
    }
  };

  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    authApi.getCurrentUser(controller.signal)
      .then(user => setCurrentUser(user))
      .catch(() => setCurrentUser(null));
    return () => controller.abort();
  }, []);

  // Live backend data & connection state
  const [liveReports, setLiveReports] = useState<ReportSummary[] | null>(null);
  const [isLiveApiConnected, setIsLiveApiConnected] = useState(false);

  const activeOrgId = activeOrganization.id;

  // Polling effect every 2.5 seconds
  useEffect(() => {
    const controller = new AbortController();

    const runPoll = () => {
      const fetchParams = currentUser?.isSuperAdmin
        ? { organizationId: activeOrgId }
        : {};

      fetchReports(fetchParams, controller.signal)
        .then(data => {
          setLiveReports(data);
          setIsLiveApiConnected(true);
          setErrorNotice('');
        })
        .catch(() => {
          if (!controller.signal.aborted) {
            setIsLiveApiConnected(false);
          }
        });
    };

    Promise.resolve().then(runPoll);
    const interval = setInterval(runPoll, 2500);

    return () => {
      controller.abort();
      clearInterval(interval);
    };
  }, [activeOrgId, currentUser]);

  // Load detail when selectedId changes
  useEffect(() => {
    if (!selectedId) return;

    const controller = new AbortController();
    Promise.resolve().then(() => {
      setLoadingDetail(true);
      fetchReportDetail(selectedId, controller.signal)
        .then(detail => {
          setSelectedDetail(detail);
        })
        .catch(() => {
          // Fallback to summary
        })
        .finally(() => {
          setLoadingDetail(false);
        });
    });

    return () => {
      controller.abort();
    };
  }, [selectedId]);

  // Combine live reports with demo fallback if live reports is null or not connected
  const allReports: DisplayReport[] = isLiveApiConnected && liveReports !== null
    ? liveReports.map(r => ({
        id: r.id,
        organizationId: r.organizationId,
        text: r.description || 'Nouzové hlášení (POTŘEBUJI POMOC)',
        place:
          r.latitude && r.longitude
            ? `GPS: ${r.latitude.toFixed(5)}, ${r.longitude.toFixed(5)}${r.accuracyMeters ? ` (±${Math.round(r.accuracyMeters)} m)` : ''}`
            : 'Poloha nezadána',
        status: r.status,
        channel: 'Klient (SOS)',
        createdAt: r.createdAt,
        isLive: true,
        reporterName:
          r.contact?.firstName || r.contact?.lastName
            ? `${r.contact?.firstName || ''} ${r.contact?.lastName || ''}`.trim()
            : undefined,
        reporterPhone: r.contact?.phone,
        reporterEmail: r.contact?.email,
        reporterPersonnelNumber: r.contact?.personnelNumber,
        latitude: r.latitude,
        longitude: r.longitude,
      }))
    : state.reports
        .filter(report => report.organizationId === state.activeOrganizationId)
        .map(r => {
          const reporter = state.users.find(u => u.id === r.reporterId);
          return {
            id: r.id,
            organizationId: r.organizationId,
            text: r.text,
            place: r.place,
            status: r.status,
            channel: r.channel,
            createdAt: r.createdAt,
            isLive: false,
            reporterName: reporter ? `${reporter.firstName} ${reporter.lastName}` : undefined,
            reporterPhone: reporter?.phone,
            reporterEmail: reporter?.email,
            reporterPersonnelNumber: reporter?.personnelNumber,
            latitude: r.latitude,
            longitude: r.longitude,
          };
        });

  const reports = allReports.filter(report => !status || report.status === status);
  const selected = allReports.find(report => report.id === selectedId);

  const refreshLiveReports = useCallback(() => {
    fetchReports({ organizationId: activeOrgId })
      .then(data => {
        setLiveReports(data);
        setIsLiveApiConnected(true);
      })
      .catch(() => {});
  }, [activeOrgId]);

  const handleStatusChange = async (nextStatus: ReportStatus) => {
    if (!selected) return;
    setIsUpdatingStatus(true);
    try {
      if (selected.isLive) {
        await updateReportStatus(selected.id, nextStatus);
        refreshLiveReports();
        if (selectedId) {
          const updated = await fetchReportDetail(selectedId);
          setSelectedDetail(updated);
        }
      } else {
        updateReport(selected.id, selected.organizationId, nextStatus);
      }
      setNotice(`Hlášení ${selected.id}: ${reportStatusMap[nextStatus].label.toLocaleLowerCase('cs')}.`);
      setSelectedId(null);
    } catch (err: unknown) {
      setErrorNotice(err instanceof Error ? err.message : 'Chyba při změně stavu');
    } finally {
      setIsUpdatingStatus(false);
    }
  };

  const columns: Column<DisplayReport>[] = [
    {
      header: 'Hlášení',
      accessor: report => (
        <>
          <div className="fw-semibold">{report.text}</div>
          <span className="small text-secondary">
            {report.id.length > 12 ? `${report.id.slice(0, 8)}...` : report.id} · {report.place}
          </span>
        </>
      ),
    },
    { header: 'Čas', accessor: report => dateTime(report.createdAt) },
    {
      header: 'Typ / Zdroj',
      accessor: report => (
        <div className="d-flex align-items-center gap-1">
          <Badge bg="light" text="dark">
            {report.channel}
          </Badge>
          {report.isLive ? (
            <Badge bg="success" className="ms-1" title="Přijato z .NET API v reálném čase">
              Live
            </Badge>
          ) : (
            <Badge bg="secondary" className="ms-1" title="Lokální ukázkový záznam">
              Demo
            </Badge>
          )}
        </div>
      ),
    },
    {
      header: 'Stav',
      accessor: report => <StatusBadge status={report.status} statusMap={reportStatusMap} />,
    },
    {
      header: 'Akce',
      className: 'text-end',
      accessor: report => (
        <Button
          variant={report.status === 'New' ? 'danger' : 'outline-primary'}
          size="sm"
          aria-label={`Detail hlášení ${report.id}`}
          onClick={() => setSelectedId(report.id)}
        >
          {report.status === 'New' ? 'Převzít / Detail' : 'Detail'}
        </Button>
      ),
    },
  ];

  return (
    <>
      <ModulePage
        title="Krizová hlášení"
        subtitle={`${activeOrganization.name} · ${isLiveApiConnected ? '🟢 Live velín (.NET API aktivní)' : '🟡 Ukázkový režim (demo data)'}`}
        noCard
      >
        {notice && (
          <div className="alert alert-success alert-dismissible fade show" role="status">
            {notice}
            <button type="button" className="btn-close" aria-label="Zavřít" onClick={() => setNotice('')} />
          </div>
        )}
        {errorNotice && (
          <div className="alert alert-danger alert-dismissible fade show" role="alert">
            {errorNotice}
            <button type="button" className="btn-close" aria-label="Zavřít" onClick={() => setErrorNotice('')} />
          </div>
        )}

        <div className="summary-strip">
          {Object.entries(reportStatusMap).map(([key, item]) => (
            <div className="summary-item" key={key}>
              <strong>{allReports.filter(report => report.status === key).length}</strong>
              <span>{item.label}</span>
            </div>
          ))}
        </div>

        {/* View Switcher Bar */}
        <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-3 pb-3 border-bottom">
          <ButtonGroup aria-label="Přepínač zobrazení velínu">
            <Button
              variant={viewMode === 'table' ? 'primary' : 'outline-secondary'}
              onClick={() => setViewMode('table')}
              className="d-flex align-items-center gap-2 fw-medium"
            >
              <span>📋 Tabulka hlášení</span>
              <Badge
                bg={viewMode === 'table' ? 'light' : 'secondary'}
                text={viewMode === 'table' ? 'dark' : undefined}
                className="rounded-pill"
              >
                {allReports.length}
              </Badge>
            </Button>
            <Button
              variant={viewMode === 'map' ? 'primary' : 'outline-secondary'}
              onClick={() => setViewMode('map')}
              className="d-flex align-items-center gap-2 fw-medium"
            >
              <span>🗺️ Mapa incidentů</span>
              <Badge
                bg={viewMode === 'map' ? 'light' : 'secondary'}
                text={viewMode === 'map' ? 'dark' : undefined}
                className="rounded-pill"
              >
                {allReports.filter(r => r.latitude != null && r.longitude != null).length}
              </Badge>
            </Button>
            <Button
              variant={viewMode === 'split' ? 'primary' : 'outline-secondary'}
              onClick={() => setViewMode('split')}
              className="d-flex align-items-center gap-1 fw-medium"
              title="Rozdělený pohled (tabulka i mapa vedle sebe)"
            >
              <span>◫ Split</span>
            </Button>
          </ButtonGroup>

          <div className="small text-secondary d-flex align-items-center gap-2">
            <span
              className={`rounded-circle d-inline-block ${isLiveApiConnected ? 'bg-success' : 'bg-warning'}`}
              style={{ width: 8, height: 8 }}
            />
            {isLiveApiConnected ? 'Live polling aktivní (2.5s)' : 'API offline – zobrazeny ukázky'}
            <Button
              variant="outline-secondary"
              size="sm"
              className="py-0 px-2"
              onClick={refreshLiveReports}
              title="Obnovit hned"
            >
              ↻
            </Button>
          </div>
        </div>

        {viewMode !== 'map' && (
          <div
            className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3"
            role="group"
            aria-label="Filtrovat hlášení podle stavu"
          >
            <div className="d-flex flex-wrap align-items-center gap-2">
              <Button
                variant={status === '' ? 'primary' : 'outline-secondary'}
                aria-pressed={status === ''}
                onClick={() => setStatus('')}
              >
                Všechna ({allReports.length})
              </Button>
              {Object.entries(reportStatusMap).map(([key, item]) => (
                <Button
                  key={key}
                  variant={status === key ? 'primary' : 'outline-secondary'}
                  aria-pressed={status === key}
                  onClick={() => setStatus(key)}
                >
                  {item.label}
                </Button>
              ))}
            </div>
          </div>
        )}

        {viewMode === 'table' && (
          <>
            <TableCustom
              data={reports}
              columns={columns}
              keyField="id"
              caption={`Hlášení organizace ${activeOrganization.name}`}
            />
            <p className="small text-secondary mt-3">
              Nová hlášení z mobilních klientů se automaticky doručují na velín a okamžitě se zobrazují ve stavu Nové.
            </p>
          </>
        )}

        {viewMode === 'map' && (
          <div className="mb-3">
            <IncidentMapModule
              reports={allReports}
              onSelectReport={setSelectedId}
              selectedReportId={selectedId}
              height="calc(100vh - 280px)"
            />
          </div>
        )}

        {viewMode === 'split' && (
          <div className="row g-3 mb-3">
            <div className="col-12 col-xl-5">
              <TableCustom
                data={reports}
                columns={columns}
                keyField="id"
                caption={`Hlášení organizace ${activeOrganization.name}`}
              />
            </div>
            <div className="col-12 col-xl-7">
              <IncidentMapModule
                reports={allReports}
                onSelectReport={setSelectedId}
                selectedReportId={selectedId}
                height="650px"
              />
            </div>
          </div>
        )}
      </ModulePage>

      <Modal
        show={!!selected}
        onHide={() => {
          setSelectedId(null);
          setListeningToStream(false);
        }}
        centered
        size="lg"
        aria-labelledby="report-detail-title"
      >
        <Modal.Header closeButton closeLabel="Zavřít">
          <Modal.Title id="report-detail-title">
            Hlášení {selected?.id} {selected?.isLive && <Badge bg="success">Live</Badge>}
          </Modal.Title>
        </Modal.Header>
        {selected && (
          <>
            <Modal.Body>
              <div className="d-flex align-items-center justify-content-between mb-3">
                <StatusBadge status={selected.status} statusMap={reportStatusMap} />
                <span className="small text-secondary">{dateTime(selected.createdAt)}</span>
              </div>

              {(() => {
                const latestStreamAction = selectedDetail?.history?.find(
                  h => h.action === 'LiveStreamStarted' || h.action === 'LiveStreamEnded'
                );
                const isLiveStreamActive = latestStreamAction?.action === 'LiveStreamStarted';
                const liveStreamMode = latestStreamAction?.newValue || 'audio';

                if (!isLiveStreamActive) return null;

                return (
                  <div className="alert alert-danger p-3 mb-3 border-danger shadow-sm">
                    <div className="d-flex align-items-center justify-content-between flex-wrap gap-2">
                      <div>
                        <div className="fw-bold d-flex align-items-center gap-2 text-danger fs-6">
                          <span className="spinner-grow spinner-grow-sm text-danger" role="status" />
                          🔴 ŽIVÝ PŘENOS Z MÍSTA AKTIVNÍ ({liveStreamMode.toUpperCase()} UPLINK)
                        </div>
                        <div className="small text-muted mt-1">
                          Oznamovatel navázal přímý LiveKit stream. Bezpečnostní pravidlo: Velín pouze odebírá signál, mikrofon velínu je deaktivován (oznamovatel vás neslyší).
                        </div>
                      </div>
                      <Button
                        variant={listeningToStream ? 'danger' : 'outline-danger'}
                        size="sm"
                        className="fw-bold"
                        onClick={() => setListeningToStream(!listeningToStream)}
                      >
                        {listeningToStream ? '⏹ Zastavit poslech' : '▶ Poslouchat přenos'}
                      </Button>
                    </div>
                    {listeningToStream && (
                      <div className="mt-3 p-2 bg-white rounded border border-danger d-flex align-items-center gap-2">
                        <div className="spinner-border spinner-border-sm text-danger" role="status" />
                        <span className="small fw-semibold text-danger">Připojeno k LiveKit místnosti incidentu (přijímám uplink v reálném čase)...</span>
                      </div>
                    )}
                  </div>
                );
              })()}

              <div className="p-3 bg-light rounded mb-3">
                <div className="small text-uppercase text-secondary fw-bold mb-1">Popis události</div>
                <div className="fs-5 fw-semibold">{selected.text}</div>
              </div>

              {(() => {
                const supplementalMessages = selectedDetail?.history?.filter(h => h.action === 'MessageAdded') || [];
                if (supplementalMessages.length === 0) return null;

                return (
                  <div className="p-3 bg-white rounded border mb-3">
                    <div className="small text-uppercase text-secondary fw-bold mb-2">
                      💬 Doplňující zprávy od oznamovatele ({supplementalMessages.length})
                    </div>
                    <div className="d-flex flex-column gap-2">
                      {supplementalMessages.map(msg => (
                        <div key={msg.id} className="p-2 bg-light rounded border-start border-3 border-primary">
                          <div className="d-flex justify-content-between align-items-start">
                            <span className="fw-medium text-dark">{msg.newValue}</span>
                            <span className="small text-muted ms-2 flex-shrink-0">{dateTime(msg.createdAt)}</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })()}

              <dl className="row mb-0">
                <dt className="col-4">Organizace</dt>
                <dd className="col-8">{activeOrganization.name}</dd>

                <dt className="col-4">Místo / Poloha</dt>
                <dd className="col-8">
                  {selected.place}
                  {selected.latitude && selected.longitude && (
                    <div className="mt-1">
                      <a
                        href={`https://www.google.com/maps?q=${selected.latitude},${selected.longitude}`}
                        target="_blank"
                        rel="noreferrer"
                        className="btn btn-sm btn-outline-secondary py-0"
                      >
                        📍 Otevřít v mapách ({selected.latitude.toFixed(5)}, {selected.longitude.toFixed(5)})
                      </a>
                    </div>
                  )}
                </dd>

                <dt className="col-4">Čas nahlášení</dt>
                <dd className="col-8">{dateTime(selected.createdAt)}</dd>

                <dt className="col-4">Oznamovatel</dt>
                <dd className="col-8">{selected.reporterName || 'Neuveden / Anonym'}</dd>

                <dt className="col-4">Telefon</dt>
                <dd className="col-8">
                  {selected.reporterPhone ? (
                    <a href={`tel:${selected.reporterPhone}`}>{selected.reporterPhone}</a>
                  ) : (
                    'Neuveden'
                  )}
                </dd>

                <dt className="col-4">E-mail</dt>
                <dd className="col-8 text-break">{selected.reporterEmail || 'Neuveden'}</dd>

                {selected.reporterPersonnelNumber && (
                  <>
                    <dt className="col-4">Osobní číslo</dt>
                    <dd className="col-8">{selected.reporterPersonnelNumber}</dd>
                  </>
                )}

                <dt className="col-4">Kanál</dt>
                <dd className="col-8">{selected.channel}</dd>
              </dl>

              {loadingDetail && (
                <div className="text-center py-3">
                  <Spinner animation="border" size="sm" className="me-2" />
                  Načítám podrobnosti z databáze...
                </div>
              )}

              {selectedDetail && selectedDetail.id === selected.id && (
                <div className="mt-4">
                  <div className="d-flex align-items-center justify-content-between border-bottom pb-2 mb-3">
                    <h6 className="fw-bold mb-0">
                      Přílohy a média {selectedDetail.attachments?.length ? `(${selectedDetail.attachments.length})` : ''}
                    </h6>
                    <div>
                      <label className="btn btn-sm btn-outline-primary mb-0 cursor-pointer">
                        <input
                          type="file"
                          className="d-none"
                          onChange={handleUploadAttachment}
                          disabled={uploadingAttachment}
                        />
                        📎 Připojit soubor
                      </label>
                      {uploadingAttachment && <Spinner animation="border" size="sm" className="ms-2" />}
                    </div>
                  </div>

                  {attachmentError && (
                    <div className="alert alert-danger py-2 small mb-3">{attachmentError}</div>
                  )}

                  {(!selectedDetail.attachments || selectedDetail.attachments.length === 0) ? (
                    <div className="text-muted small fst-italic mb-3">K tomuto hlášení nejsou přiloženy žádné fotografie ani hlasové zprávy.</div>
                  ) : (
                    <div className="row g-3 mb-3">
                      {selectedDetail.attachments.map(att => {
                        const isImage = att.contentType.startsWith('image/');
                        const isAudio = att.contentType.startsWith('audio/') || att.fileName.endsWith('.m4a') || att.fileName.endsWith('.mp3');
                        const fileUrl = `/api/reports/${selected.id}/attachments/${att.id}`;

                        return (
                          <div key={att.id} className="col-12 col-md-6">
                            <div className="card h-100 border p-2">
                              {isImage && (
                                <a href={fileUrl} target="_blank" rel="noreferrer" className="d-block text-center mb-2">
                                  {/* eslint-disable-next-line @next/next/no-img-element */}
                                  <img
                                    src={fileUrl}
                                    alt={att.fileName}
                                    className="img-fluid rounded border"
                                    style={{ maxHeight: '160px', objectFit: 'contain' }}
                                  />
                                </a>
                              )}
                              {isAudio && (
                                <div className="mb-2">
                                  <span className="badge bg-secondary mb-1">🎤 Hlasová zpráva</span>
                                  <audio controls className="w-100" src={fileUrl} preload="metadata">
                                    Váš prohlížeč nepodporuje přehrávání audia.
                                  </audio>
                                </div>
                              )}
                              <div className="d-flex justify-content-between align-items-center mt-auto pt-1 border-top small">
                                <span className="text-truncate fw-medium" title={att.fileName}>{att.fileName}</span>
                                <div className="d-flex align-items-center gap-2 ms-2 flex-shrink-0">
                                  <span className="text-muted">{formatBytes(att.sizeBytes)}</span>
                                  <a href={fileUrl} download={att.fileName} className="btn btn-sm btn-link p-0 text-decoration-none">
                                    ⬇ Stáhnout
                                  </a>
                                </div>
                              </div>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}

              {selectedDetail && selectedDetail.id === selected.id && selectedDetail.history && selectedDetail.history.length > 0 && (
                <div className="mt-4">
                  <h6 className="fw-bold border-bottom pb-2">Historie hlášení</h6>
                  <ul className="list-unstyled small mb-0">
                    {selectedDetail.history.map(item => {
                      const actionLabels: Record<string, string> = {
                        Created: 'Založeno',
                        StatusChange: 'Změna stavu',
                        LocationUpdated: 'Zpřesněna GPS poloha',
                        AttachmentAdded: 'Přidána příloha',
                        MessageAdded: 'Doplňující zpráva',
                        LiveStreamStarted: 'Zahájen živý přenos',
                        LiveStreamEnded: 'Ukončen živý přenos',
                      };
                      return (
                        <li key={item.id} className="mb-2 d-flex justify-content-between align-items-start">
                          <div>
                            <strong>{actionLabels[item.action] || item.action}</strong>
                            {item.newValue && (
                              <span className="ms-1 text-muted">
                                {item.previousValue ? `${item.previousValue} → ` : ''}
                                {item.newValue}
                              </span>
                            )}
                            {item.metadata && <div className="text-muted fst-italic">{item.metadata}</div>}
                          </div>
                          <span className="text-muted">{dateTime(item.createdAt)}</span>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button
                variant="outline-secondary"
                onClick={() => {
                  setSelectedId(null);
                  setListeningToStream(false);
                }}
              >
                Zavřít
              </Button>
              {selected.status === 'New' && (
                <Button
                  variant="warning"
                  disabled={isUpdatingStatus}
                  onClick={() => handleStatusChange('Accepted')}
                >
                  {isUpdatingStatus ? <Spinner animation="border" size="sm" /> : 'Převzít hlášení'}
                </Button>
              )}
              {selected.status === 'Accepted' && (
                <Button
                  variant="success"
                  disabled={isUpdatingStatus}
                  onClick={() => handleStatusChange('Closed')}
                >
                  {isUpdatingStatus ? <Spinner animation="border" size="sm" /> : 'Uzavřít hlášení'}
                </Button>
              )}
            </Modal.Footer>
          </>
        )}
      </Modal>
    </>
  );
}
