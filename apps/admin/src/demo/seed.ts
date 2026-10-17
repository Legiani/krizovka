import type { DemoState } from './types';

// Entirely fictional records. No source databases, real contact details or media.
export const initialDemoState: DemoState = {
  version: 1,
  activeOrganizationId: '11111111-1111-1111-1111-111111111111',
  organizations: [
    { id: '11111111-1111-1111-1111-111111111111', code: 'KAMPUS', name: 'Ukázkový kampus Sever', contactEmail: 'velin@kampus.example.invalid', status: 'ACTIVE' },
    { id: '22222222-2222-2222-2222-222222222222', code: 'CENTRUM', name: 'Ukázkové administrativní centrum', contactEmail: 'recepce@centrum.example.invalid', status: 'ACTIVE' },
    { id: '33333333-3333-3333-3333-333333333333', code: 'SKLAD', name: 'Ukázkový logistický areál', contactEmail: 'velin@sklad.example.invalid', status: 'PLANNED' },
  ],
  users: [
    { id: 'user-1', firstName: 'Anna', lastName: 'Ukázková', email: 'anna@kampus.example.invalid', phone: '', personnelNumber: 'DEMO-001', isActive: true, memberships: [{ organizationId: '11111111-1111-1111-1111-111111111111', role: 'ADMIN' }, { organizationId: '22222222-2222-2222-2222-222222222222', role: 'OPERATOR' }] },
    { id: 'user-2', firstName: 'Petr', lastName: 'Zkušební', email: 'petr@kampus.example.invalid', phone: '', personnelNumber: 'DEMO-002', isActive: true, memberships: [{ organizationId: '11111111-1111-1111-1111-111111111111', role: 'OPERATOR' }] },
    { id: 'user-3', firstName: 'Klára', lastName: 'Modelová', email: 'klara@kampus.example.invalid', phone: '', personnelNumber: 'DEMO-003', isActive: true, memberships: [{ organizationId: '11111111-1111-1111-1111-111111111111', role: 'USER' }] },
    { id: 'user-4', firstName: 'Tomáš', lastName: 'Testovací', email: 'tomas@kampus.example.invalid', phone: '', personnelNumber: 'DEMO-004', isActive: false, memberships: [{ organizationId: '11111111-1111-1111-1111-111111111111', role: 'USER' }] },
    { id: 'user-5', firstName: 'Eva', lastName: 'Ukázková', email: 'eva@centrum.example.invalid', phone: '', personnelNumber: 'DEMO-005', isActive: true, memberships: [{ organizationId: '22222222-2222-2222-2222-222222222222', role: 'ADMIN' }] },
    { id: 'user-6', firstName: 'David', lastName: 'Modelový', email: 'david@centrum.example.invalid', phone: '', personnelNumber: 'DEMO-006', isActive: true, memberships: [{ organizationId: '22222222-2222-2222-2222-222222222222', role: 'USER' }] },
    { id: 'user-7', firstName: 'Lucie', lastName: 'Zkušební', email: 'lucie@sklad.example.invalid', phone: '', personnelNumber: 'DEMO-007', isActive: true, memberships: [{ organizationId: '33333333-3333-3333-3333-333333333333', role: 'ADMIN' }] },
  ],
  reports: [
    { id: 'DEMO-101', organizationId: '11111111-1111-1111-1111-111111111111', reporterId: 'user-3', text: 'Z chodby je cítit kouř. Prosím o prověření.', place: 'Budova A · 2. patro', status: 'New', channel: 'Text', createdAt: '2026-10-17T08:42:00+02:00', latitude: 50.08804, longitude: 14.42076 },
    { id: 'DEMO-102', organizationId: '11111111-1111-1111-1111-111111111111', reporterId: 'user-1', text: 'Ukázka doručené hlasové zprávy.', place: 'Budova B · vstupní hala', status: 'Accepted', channel: 'Hlasovka', createdAt: '2026-10-17T08:35:00+02:00', latitude: 50.08638, longitude: 14.42382 },
    { id: 'DEMO-103', organizationId: '11111111-1111-1111-1111-111111111111', reporterId: 'user-2', text: 'Ukázka hlášení s přímým video přenosem.', place: 'Kampus · severní vstup', status: 'New', channel: 'Živě video', createdAt: '2026-10-17T08:30:00+02:00', latitude: 50.08950, longitude: 14.41700 },
    { id: 'DEMO-104', organizationId: '11111111-1111-1111-1111-111111111111', reporterId: 'user-3', text: 'Zablokovaný průchod byl uvolněn.', place: 'Budova C · přízemí', status: 'Closed', channel: 'Text', createdAt: '2026-10-17T07:50:00+02:00', latitude: 50.08420, longitude: 14.42150 },
    { id: 'DEMO-201', organizationId: '22222222-2222-2222-2222-222222222222', reporterId: 'user-6', text: 'Nefunguje osvětlení na schodišti.', place: 'Centrum · schodiště západ', status: 'New', channel: 'Text', createdAt: '2026-10-17T08:38:00+02:00', latitude: 50.07553, longitude: 14.43780 },
    { id: 'DEMO-202', organizationId: '22222222-2222-2222-2222-222222222222', reporterId: 'user-5', text: 'Ukázka přímého audio přenosu do velínu.', place: 'Centrum · recepce', status: 'Accepted', channel: 'Živě audio', createdAt: '2026-10-17T08:20:00+02:00', latitude: 50.07312, longitude: 14.43950 },
  ],
  guides: [
    { id: 'guide-1', organizationId: '11111111-1111-1111-1111-111111111111', title: 'Evakuace budovy A', summary: 'Ukázka organizačního manuálu pro zaměstnance a studenty.', status: 'Published', source: 'Převzato z ukázkové knihovny' },
    { id: 'guide-2', organizationId: '11111111-1111-1111-1111-111111111111', title: 'Kontakty a místa pomoci', summary: 'Přehled místní obsluhy, kontaktních míst a provozních informací.', status: 'Published', source: 'Vlastní metodika organizace' },
    { id: 'guide-3', organizationId: '11111111-1111-1111-1111-111111111111', title: 'Postup pro recepci', summary: 'Rozpracovaný ukázkový obsah čekající na kontrolu správce.', status: 'Draft', source: 'Vlastní metodika organizace' },
    { id: 'guide-4', organizationId: '22222222-2222-2222-2222-222222222222', title: 'Evakuace centra', summary: 'Ukázkový manuál administrativního centra.', status: 'Published', source: 'Převzato z ukázkové knihovny' },
    { id: 'guide-5', organizationId: '22222222-2222-2222-2222-222222222222', title: 'Kontaktní osoby', summary: 'Ukázkový přehled odpovědných osob v objektu.', status: 'Draft', source: 'Vlastní metodika organizace' },
  ],
};
