export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  token?: string | null;
  errorMessage?: string | null;
  userId?: string | null;
  identityId?: string | null;
  roleCode?: string | null;
}

export interface RoleInfo {
  roleId: string;
  name: string;
  code: string;
  description?: string;
}

export interface PermissionInfo {
  module: string;
  action: string;
  scopeType: string;
  scopeValue: string;
}

export interface CurrentUser {
  userId: string;
  identityId: string;
  accountId: string;
  email: string;
  firstName: string;
  lastName: string;
  isSuperAdmin: boolean;
  organizationId?: string | null;
  role?: RoleInfo | null;
  permissions: PermissionInfo[];
}
