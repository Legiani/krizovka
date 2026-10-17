import {
  BackofficeUser,
  BackofficeRole,
  CreateBackofficeUserRequest,
  UpdateBackofficeUserRequest,
  CreateRoleRequest,
  AddPermissionRequest
} from './types';

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let msg = `Chyba požadavku: HTTP ${res.status}`;
    try {
      const err = await res.json();
      msg = err.detail || err.title || err.message || err.error || msg;
    } catch {
      // ignore json parse failure
    }
    throw new Error(msg);
  }
  if (res.status === 204) return {} as T;
  return res.json() as Promise<T>;
}

export const accessControlApi = {
  // Users
  async getUsers(signal?: AbortSignal): Promise<BackofficeUser[]> {
    const res = await fetch('/api/backoffice/users', { signal, cache: 'no-store' });
    const data = await handleResponse<Record<string, unknown>[]>(res);
    return data.map(u => ({
      id: String(u.id || ''),
      identityId: String(u.identityId || u.id || ''),
      email: String(u.email || ''),
      firstName: String(u.firstName || ''),
      lastName: String(u.lastName || ''),
      isSuperAdmin: Boolean(u.isSuperAdmin),
      roleId: u.roleId ? String(u.roleId) : undefined,
      roleName: u.roleName ? String(u.roleName) : undefined,
      roleCode: u.roleCode ? String(u.roleCode) : undefined,
      accountId: u.accountId ? String(u.accountId) : undefined,
      permissions: Array.isArray(u.permissions) ? (u.permissions as string[]) : [],
    }));
  },

  async getUser(id: string, signal?: AbortSignal): Promise<BackofficeUser> {
    const res = await fetch(`/api/backoffice/users/${id}`, { signal, cache: 'no-store' });
    return handleResponse<BackofficeUser>(res);
  },

  async createUser(data: CreateBackofficeUserRequest): Promise<{ id: string }> {
    const res = await fetch('/api/backoffice/users', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        workingEmail: data.email,
        firstName: data.firstName,
        lastName: data.lastName,
        roleId: data.roleId || null,
        isSuperAdmin: data.isSuperAdmin,
        accountId: data.accountId || null,
      }),
    });
    return handleResponse<{ id: string }>(res);
  },

  async updateUser(data: UpdateBackofficeUserRequest): Promise<void> {
    const res = await fetch(`/api/backoffice/users/${data.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        id: data.id,
        workingEmail: data.email,
        firstName: data.firstName || '',
        lastName: data.lastName || '',
        roleId: data.roleId || null,
        isSuperAdmin: data.isSuperAdmin,
      }),
    });
    return handleResponse<void>(res);
  },

  async deleteUser(id: string): Promise<void> {
    const res = await fetch(`/api/backoffice/users/${id}`, { method: 'DELETE' });
    return handleResponse<void>(res);
  },

  async resetPassword(id: string, newPassword: string): Promise<void> {
    const res = await fetch(`/api/backoffice/users/${id}/reset-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newPassword }),
    });
    return handleResponse<void>(res);
  },

  // Roles
  async getRoles(signal?: AbortSignal): Promise<BackofficeRole[]> {
    const res = await fetch('/api/backoffice/roles', { signal, cache: 'no-store' });
    const data = await handleResponse<{ roles?: BackofficeRole[] } | BackofficeRole[]>(res);
    if (Array.isArray(data)) return data;
    return data.roles || [];
  },

  async createRole(data: CreateRoleRequest): Promise<{ id: string }> {
    const res = await fetch('/api/backoffice/roles', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return handleResponse<{ id: string }>(res);
  },

  async deleteRole(id: string): Promise<void> {
    const res = await fetch(`/api/backoffice/roles/${id}`, { method: 'DELETE' });
    return handleResponse<void>(res);
  },

  async addPermission(roleId: string, perm: AddPermissionRequest): Promise<void> {
    const res = await fetch(`/api/backoffice/roles/${roleId}/permissions`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(perm),
    });
    return handleResponse<void>(res);
  },

  async removePermission(permId: string): Promise<void> {
    const res = await fetch(`/api/backoffice/roles/permissions/${permId}`, { method: 'DELETE' });
    return handleResponse<void>(res);
  },
};
