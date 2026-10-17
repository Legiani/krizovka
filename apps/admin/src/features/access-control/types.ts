export interface Permission {
  id: string;
  module: string;
  action: string;
  scopeType: string;
  scopeValue: string;
}

export interface BackofficeRole {
  id: string;
  name: string;
  code: string;
  description?: string;
  permissions: Permission[];
}

export interface BackofficeUser {
  id: string;
  identityId: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roleId?: string;
  roleName?: string;
  roleCode?: string;
  permissions?: string[];
  isSuperAdmin: boolean;
  accountId?: string;
}

export interface CreateBackofficeUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  roleId?: string | null;
  isSuperAdmin: boolean;
  accountId?: string | null;
}

export interface UpdateBackofficeUserRequest {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roleId?: string | null;
  isSuperAdmin: boolean;
}

export interface CreateRoleRequest {
  name: string;
  code: string;
  description?: string;
}

export interface AddPermissionRequest {
  module: string;
  action: string;
  scopeType: string;
  scopeValue: string;
}
