export type Role = 'ADMIN' | 'OPERATOR' | 'USER';
export type OrganizationStatus = 'ACTIVE' | 'PLANNED' | 'INACTIVE';
export type ReportStatus = 'New' | 'Accepted' | 'Closed';

export interface Organization {
  id: string;
  code: string;
  name: string;
  contactEmail: string;
  status: OrganizationStatus;
}

export interface Membership {
  organizationId: string;
  role: Role;
}

export interface DemoUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  personnelNumber: string;
  isActive: boolean;
  memberships: Membership[];
}

export interface DemoReport {
  id: string;
  organizationId: string;
  reporterId: string;
  text: string;
  place: string;
  status: ReportStatus;
  channel: 'Text' | 'Hlasovka' | 'Živě audio' | 'Živě video';
  createdAt: string;
  latitude?: number;
  longitude?: number;
}

export interface DemoGuide {
  id: string;
  organizationId: string;
  title: string;
  summary: string;
  status: 'Published' | 'Draft';
  source: string;
}

export interface DemoState {
  version: 1;
  activeOrganizationId: string;
  organizations: Organization[];
  users: DemoUser[];
  reports: DemoReport[];
  guides: DemoGuide[];
}

export const roleLabels: Record<Role, string> = {
  ADMIN: 'Správce', OPERATOR: 'Operátor', USER: 'Uživatel',
};

export const reportStatusMap = {
  New: { label: 'Nové', variant: 'danger' },
  Accepted: { label: 'Převzaté', variant: 'warning' },
  Closed: { label: 'Uzavřené', variant: 'secondary' },
};
