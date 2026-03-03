const BASE_URL = import.meta.env.VITE_PORTAL_API_URL ?? 'http://localhost:5001'

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
}

export interface ApiKeyDto {
  id: string
  label: string
  prefix: string
  createdAt: string
  revokedAt: string | null
}

export interface CreateKeyRequest {
  label: string
}

export interface CreateKeyResponse {
  id: string
  label: string
  rawKey: string
  prefix: string
  createdAt: string
}

export interface UserSummaryDto {
  id: string
  email: string
  role: string
  createdAt: string
  keyCount: number
}

class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers })

  if (!res.ok) {
    let message = res.statusText
    try {
      const body = await res.json()
      message = body?.error?.message ?? message
    } catch {
      // ignore parse errors
    }
    throw new ApiError(res.status, message)
  }

  const text = await res.text()
  return text ? (JSON.parse(text) as T) : ({} as T)
}

export const api = {
  register: (data: RegisterRequest) =>
    request<void>('/auth/register', { method: 'POST', body: JSON.stringify(data) }),

  login: (data: LoginRequest) =>
    request<LoginResponse>('/auth/login', { method: 'POST', body: JSON.stringify(data) }),

  listKeys: (token: string) =>
    request<ApiKeyDto[]>('/api/keys', {}, token),

  createKey: (data: CreateKeyRequest, token: string) =>
    request<CreateKeyResponse>('/api/keys', { method: 'POST', body: JSON.stringify(data) }, token),

  revokeKey: (id: string, token: string) =>
    request<void>(`/api/keys/${id}`, { method: 'DELETE' }, token),

  listUsers: (token: string) =>
    request<UserSummaryDto[]>('/admin/users', {}, token),

  deleteUser: (id: string, token: string) =>
    request<void>(`/admin/users/${id}`, { method: 'DELETE' }, token),
}
