export interface PermissionAction {
  code: string;
  label: string;
  description: string;
}

export interface PermissionModule {
  code: string;
  label: string;
  icon: string;
  actions: PermissionAction[];
}

export const PERMISSION_CATALOG: PermissionModule[] = [
  {
    code: 'REPORTS',
    label: 'Krizová hlášení',
    icon: '🚨',
    actions: [
      { code: 'READ', label: 'Čtení', description: 'Prohlížení seznamu a detailu krizových hlášení' },
      { code: 'WRITE', label: 'Převzetí a řešení', description: 'Potvrzení převzetí hlášení a změna stavu' },
    ],
  },
  {
    code: 'USERS',
    label: 'Uživatelé a velín',
    icon: '👤',
    actions: [
      { code: 'READ', label: 'Čtení', description: 'Prohlížení administrátorů a operátorů' },
      { code: 'WRITE', label: 'Editace', description: 'Vytváření a úprava účtů' },
      { code: 'DELETE', label: 'Mazání', description: 'Mazání účtů' },
      { code: 'RESET_PASSWORD', label: 'Reset hesla', description: 'Resetování hesla účtu' },
    ],
  },
  {
    code: 'ROLES',
    label: 'Role a oprávnění',
    icon: '🔑',
    actions: [
      { code: 'READ', label: 'Čtení', description: 'Prohlížení rolí a oprávnění' },
      { code: 'WRITE', label: 'Editace', description: 'Vytváření a úprava rolí' },
      { code: 'DELETE', label: 'Mazání', description: 'Mazání rolí' },
    ],
  },
  {
    code: 'ORGANIZATIONS',
    label: 'Organizace',
    icon: '🏢',
    actions: [
      { code: 'READ', label: 'Čtení', description: 'Prohlížení organizací' },
      { code: 'WRITE', label: 'Editace', description: 'Správa parametrů organizace' },
    ],
  },
  {
    code: 'METHODOLOGIES',
    label: 'Krizové metodiky',
    icon: '📖',
    actions: [
      { code: 'READ', label: 'Čtení', description: 'Čtení krizových manuálů' },
      { code: 'WRITE', label: 'Editace', description: 'Správa a publikace metodik organizace' },
    ],
  },
];

export const getModuleLabel = (code: string): string =>
  PERMISSION_CATALOG.find(m => m.code === code)?.label ?? code;

export const getModuleIcon = (code: string): string =>
  PERMISSION_CATALOG.find(m => m.code === code)?.icon ?? '🔒';

export const getActionLabel = (module: string, action: string): string =>
  PERMISSION_CATALOG.find(m => m.code === module)
    ?.actions.find(a => a.code === action)?.label ?? action;

export const SYSTEM_ROLES = ['BO_SUPER_ADMIN', 'BO_ADMIN', 'BO_USER', 'KRIZOVKA_OPERATOR'];
export const isSystemRole = (code: string): boolean => SYSTEM_ROLES.includes(code);
