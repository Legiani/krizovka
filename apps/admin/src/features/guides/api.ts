export interface GuideStep {
  stepNumber: number;
  title: string;
  description: string;
  isDoNot?: boolean;
}

export interface EmergencyContact {
  name: string;
  phone: string;
  role: string;
}

export interface LegalBasisItem {
  title: string;
  lawNumber?: string;
  section?: string;
  issuer?: string;
  url?: string;
}

export interface GuideTemplate {
  id: string;
  title: string;
  category: string;
  summary: string;
  quickSummary?: string;
  diagramMermaid?: string;
  legalBasisJson?: string;
  recommendedOrgTypesJson?: string;
  version: number;
  stepsJson: string;
  emergencyContactsJson: string;
  createdAt: string;
}

export interface Guide {
  id: string;
  organizationId: string;
  sourceTemplateId?: string;
  sourceVersion?: number;
  title: string;
  category: string;
  summary: string;
  quickSummary?: string;
  diagramMermaid?: string;
  legalBasisJson?: string;
  status: 'Draft' | 'Published' | 'Archived';
  stepsJson: string;
  emergencyContactsJson: string;
  publishedAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export async function fetchGuideTemplates(signal?: AbortSignal): Promise<GuideTemplate[]> {
  const res = await fetch('/api/guide-templates', {
    method: 'GET',
    headers: { Accept: 'application/json' },
    signal,
  });
  if (!res.ok) throw new Error(`Chyba při načítání šablon metodik (${res.status})`);
  return res.json();
}

export async function fetchGuides(
  params?: { organizationId?: string; status?: string },
  signal?: AbortSignal
): Promise<Guide[]> {
  const query = new URLSearchParams();
  if (params?.organizationId) query.set('organizationId', params.organizationId);
  if (params?.status) query.set('status', params.status);

  const url = `/api/guides${query.toString() ? `?${query.toString()}` : ''}`;
  const res = await fetch(url, {
    method: 'GET',
    headers: { Accept: 'application/json' },
    signal,
  });
  if (!res.ok) throw new Error(`Chyba při načítání metodik (${res.status})`);
  return res.json();
}

export async function fetchGuideDetail(id: string, signal?: AbortSignal): Promise<Guide> {
  const res = await fetch(`/api/guides/${id}`, {
    method: 'GET',
    headers: { Accept: 'application/json' },
    signal,
  });
  if (!res.ok) throw new Error(`Chyba při načítání metodiky (${res.status})`);
  return res.json();
}

export async function createGuide(data: {
  organizationId: string;
  title: string;
  category: string;
  summary?: string;
  quickSummary?: string;
  diagramMermaid?: string;
  legalBasisJson?: string;
  stepsJson?: string;
  emergencyContactsJson?: string;
}): Promise<Guide> {
  const res = await fetch('/api/guides', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při vytváření metodiky (${res.status})`);
  }
  return res.json();
}

export async function updateGuide(
  id: string,
  data: {
    title: string;
    category: string;
    summary: string;
    quickSummary?: string;
    diagramMermaid?: string;
    legalBasisJson?: string;
    status?: 'Draft' | 'Published' | 'Archived';
    stepsJson?: string;
    emergencyContactsJson?: string;
  }
): Promise<Guide> {
  const res = await fetch(`/api/guides/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při ukládání metodiky (${res.status})`);
  }
  return res.json();
}

export async function cloneGuideFromTemplate(
  templateId: string,
  organizationId: string
): Promise<Guide> {
  const res = await fetch('/api/guides/clone', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify({ templateId, organizationId }),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při klonování metodiky (${res.status})`);
  }
  return res.json();
}

export async function batchCloneGuidesFromTemplates(
  templateIds: string[],
  organizationId: string
): Promise<Guide[]> {
  const res = await fetch('/api/guides/clone-batch', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify({ templateIds, organizationId }),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při hromadném klonování metodik (${res.status})`);
  }
  return res.json();
}

export async function publishGuide(id: string): Promise<Guide> {
  const res = await fetch(`/api/guides/${id}/publish`, {
    method: 'POST',
    headers: { Accept: 'application/json' },
  });
  if (!res.ok) throw new Error(`Chyba při publikaci metodiky (${res.status})`);
  return res.json();
}

export async function archiveGuide(id: string): Promise<Guide> {
  const res = await fetch(`/api/guides/${id}/archive`, {
    method: 'POST',
    headers: { Accept: 'application/json' },
  });
  if (!res.ok) throw new Error(`Chyba při archivaci metodiky (${res.status})`);
  return res.json();
}

export async function deleteGuide(id: string): Promise<void> {
  const res = await fetch(`/api/guides/${id}`, {
    method: 'DELETE',
  });
  if (!res.ok) throw new Error(`Chyba při mazání metodiky (${res.status})`);
}
