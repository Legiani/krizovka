'use client';

import { useSyncExternalStore } from 'react';
import { initialDemoState } from './seed';
import type { DemoState, DemoUser, Organization } from './types';

const storageKey = 'krizovka.admin.demo.v2';
const listeners = new Set<() => void>();
let snapshot = initialDemoState;
let loaded = false;
let storageFailed = false;

function readSnapshot() {
  if (!loaded && typeof window !== 'undefined') {
    loaded = true;
    try {
      const raw = window.localStorage.getItem(storageKey);
      const stored = raw ? JSON.parse(raw) as DemoState : null;
      if (stored?.version === 1 && Array.isArray(stored.organizations) && stored.organizations.length &&
          Array.isArray(stored.users) && Array.isArray(stored.reports) && Array.isArray(stored.guides) &&
          stored.organizations.some(org => org.id === stored.activeOrganizationId)) snapshot = stored;
    } catch { storageFailed = true; }
  }
  return snapshot;
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  const sync = (event: StorageEvent) => {
    if (event.key === storageKey || event.key === null) {
      snapshot = initialDemoState;
      loaded = false;
      listener();
    }
  };
  window.addEventListener('storage', sync);
  return () => { listeners.delete(listener); window.removeEventListener('storage', sync); };
}

function update(change: (state: DemoState) => DemoState) {
  const next = change(readSnapshot());
  snapshot = next;
  try { window.localStorage.setItem(storageKey, JSON.stringify(next)); storageFailed = false; }
  catch { storageFailed = true; }
  listeners.forEach(listener => listener());
}

export function useDemo() {
  const state = useSyncExternalStore(subscribe, readSnapshot, () => initialDemoState);
  return { state, storageFailed, activeOrganization: state.organizations.find(org => org.id === state.activeOrganizationId)! };
}

export function selectOrganization(id: string) {
  update(state => state.organizations.some(org => org.id === id) ? { ...state, activeOrganizationId: id } : state);
}

export function saveOrganization(organization: Organization) {
  update(state => {
    if (state.organizations.some(org => org.id !== organization.id && org.code.toUpperCase() === organization.code.toUpperCase()))
      throw new Error('Organizace s tímto kódem již existuje.');
    const exists = state.organizations.some(org => org.id === organization.id);
    return { ...state, organizations: exists ? state.organizations.map(org => org.id === organization.id ? organization : org) : [...state.organizations, organization] };
  });
}

export function saveUser(user: DemoUser) {
  update(state => {
    if (state.users.some(item => item.id !== user.id && item.email.toLowerCase() === user.email.toLowerCase()))
      throw new Error('Uživatel s tímto e-mailem již existuje. Upravte jeho členství.');
    if (!user.memberships.length) throw new Error('Vyberte alespoň jednu organizaci.');
    const exists = state.users.some(item => item.id === user.id);
    return { ...state, users: exists ? state.users.map(item => item.id === user.id ? user : item) : [...state.users, user] };
  });
}

export function updateReport(id: string, organizationId: string, status: DemoState['reports'][number]['status']) {
  update(state => ({ ...state, reports: state.reports.map(report =>
    report.id === id && report.organizationId === organizationId ? { ...report, status } : report) }));
}

export function resetDemo() {
  update(() => structuredClone(initialDemoState));
}
