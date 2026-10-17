export interface OrganizationArea {
  id: string;
  organizationId: string;
  name: string;
  polygonGeoJson: string;
  offsetMeters: number;
  allocatedVelinId: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ApiOrganization {
  id: string;
  name: string;
  code: string;
  status: string;
  contactEmail?: string;
  contactPhone?: string;
  isDefaultDispatch: boolean;
  createdAt: string;
  updatedAt?: string;
  areas: OrganizationArea[];
}

export async function fetchOrganizations(signal?: AbortSignal): Promise<ApiOrganization[]> {
  const res = await fetch('/api/organizations', {
    method: 'GET',
    headers: { Accept: 'application/json' },
    signal,
  });
  if (!res.ok) throw new Error(`Chyba při načítání organizací (${res.status})`);
  return res.json();
}

export async function createOrganization(data: {
  name: string;
  code: string;
  status?: string;
  contactEmail?: string;
  contactPhone?: string;
  isDefaultDispatch?: boolean;
}): Promise<ApiOrganization> {
  const res = await fetch('/api/organizations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při vytváření organizace (${res.status})`);
  }
  return res.json();
}

export async function updateOrganization(
  id: string,
  data: {
    name: string;
    code: string;
    status: string;
    contactEmail?: string;
    contactPhone?: string;
    isDefaultDispatch: boolean;
  }
): Promise<ApiOrganization> {
  const res = await fetch(`/api/organizations/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při úpravě organizace (${res.status})`);
  }
  return res.json();
}

export async function upsertOrganizationArea(
  organizationId: string,
  data: {
    name: string;
    polygonGeoJson: string;
    offsetMeters: number;
    allocatedVelinId: string;
  }
): Promise<OrganizationArea> {
  const res = await fetch(`/api/organizations/${organizationId}/areas`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const errorBody = await res.json().catch(() => null);
    throw new Error(errorBody?.error || `Chyba při ukládání areálu (${res.status})`);
  }
  return res.json();
}

export async function deleteOrganizationArea(
  organizationId: string,
  areaId: string
): Promise<void> {
  const res = await fetch(`/api/organizations/${organizationId}/areas/${areaId}`, {
    method: 'DELETE',
  });
  if (!res.ok) throw new Error(`Chyba při mazání areálu (${res.status})`);
}
