export type ReportStatus = 'New' | 'Accepted' | 'Closed';

export interface ContactSnapshot {
  firstName?: string;
  lastName?: string;
  personnelNumber?: string;
  phone?: string;
  email?: string;
}

export interface ReportSummary {
  id: string;
  organizationId: string;
  clientReportId: string;
  description?: string;
  status: ReportStatus;
  createdAt: string;
  updatedAt?: string;
  latitude?: number;
  longitude?: number;
  accuracyMeters?: number;
  locationMeasuredAt?: string;
  contact?: ContactSnapshot;
}

export interface ReportHistoryItem {
  id: string;
  actorId: string;
  action: string;
  previousValue?: string;
  newValue?: string;
  metadata?: string;
  createdAt: string;
}

export interface ReportAttachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  storageKey: string;
  createdAt: string;
}

export interface ReportDetail extends ReportSummary {
  reporterAccountId: string;
  history: ReportHistoryItem[];
  attachments: ReportAttachment[];
}

export async function fetchReports(
  params?: { organizationId?: string; status?: string },
  signal?: AbortSignal
): Promise<ReportSummary[]> {
  const query = new URLSearchParams();
  if (params?.organizationId) query.set('organizationId', params.organizationId);
  if (params?.status) query.set('status', params.status);

  const url = `/api/reports${query.toString() ? `?${query.toString()}` : ''}`;
  const res = await fetch(url, {
    signal,
    headers: { Accept: 'application/json' },
    cache: 'no-store',
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch reports: ${res.statusText}`);
  }

  return res.json();
}

export async function fetchReportDetail(id: string, signal?: AbortSignal): Promise<ReportDetail> {
  const res = await fetch(`/api/reports/${id}`, {
    signal,
    headers: { Accept: 'application/json' },
    cache: 'no-store',
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch report detail: ${res.statusText}`);
  }

  return res.json();
}

export async function updateReportStatus(
  id: string,
  status: ReportStatus,
  note?: string
): Promise<ReportDetail> {
  const res = await fetch(`/api/reports/${id}/status`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify({ status, note }),
  });

  if (!res.ok) {
    throw new Error(`Failed to update report status: ${res.statusText}`);
  }

  return res.json();
}

export async function uploadReportAttachment(
  id: string,
  file: File
): Promise<ReportAttachment> {
  const formData = new FormData();
  formData.append('file', file);

  const res = await fetch(`/api/reports/${id}/attachments`, {
    method: 'POST',
    body: formData,
  });

  if (!res.ok) {
    let msg = `Chyba při nahrávání přílohy: ${res.statusText}`;
    try {
      const err = await res.json();
      msg = err.error || err.detail || msg;
    } catch {
      // fallback
    }
    throw new Error(msg);
  }

  return res.json();
}
