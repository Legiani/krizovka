import type { ReportStatus } from '@/demo/types';

export interface DisplayReport {
  id: string;
  organizationId: string;
  text: string;
  place: string;
  status: ReportStatus;
  channel: string;
  createdAt: string;
  isLive: boolean;
  reporterName?: string;
  reporterPhone?: string;
  reporterEmail?: string;
  reporterPersonnelNumber?: string;
  latitude?: number;
  longitude?: number;
}

export interface SelectedMapReport {
  id: string;
  title: string;
  description: string;
  place: string;
  status: ReportStatus;
  channel: string;
  createdAt: string;
  isLive: boolean;
  latitude: number;
  longitude: number;
  reporterName?: string;
  reporterPhone?: string;
}
