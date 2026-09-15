import type { LoginResult } from '../types/auth'

const SESSION_STORAGE_KEY = 'sistema-centinela-session'

export function getSession(): LoginResult | undefined {
  const raw = sessionStorage.getItem(SESSION_STORAGE_KEY)

  if (!raw) {
    return undefined
  }

  try {
    return JSON.parse(raw) as LoginResult
  } catch {
    sessionStorage.removeItem(SESSION_STORAGE_KEY)
    return undefined
  }
}

export function setSession(session: LoginResult): void {
  sessionStorage.setItem(
    SESSION_STORAGE_KEY,
    JSON.stringify(session),
  )
}

export function clearSession(): void {
  sessionStorage.removeItem(SESSION_STORAGE_KEY)
}

export function isSessionExpired(session: LoginResult): boolean {
  return new Date(session.expiresAt).getTime() <= Date.now()
}
