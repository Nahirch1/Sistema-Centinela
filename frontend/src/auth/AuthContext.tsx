import {
  createContext,
  useContext,
  useState,
  type ReactNode,
} from 'react'

import { login as loginRequest } from '../api/auth'
import type { LoginResult } from '../types/auth'

import {
  clearSession,
  getSession,
  isSessionExpired,
  setSession,
} from './session'

interface AuthContextValue {
  session: LoginResult | undefined
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(
  undefined,
)

function readValidSession(): LoginResult | undefined {
  const stored = getSession()

  if (!stored) {
    return undefined
  }

  if (isSessionExpired(stored)) {
    clearSession()
    return undefined
  }

  return stored
}

export function AuthProvider({
  children,
}: {
  children: ReactNode
}) {
  const [session, setSessionState] = useState<LoginResult | undefined>(
    readValidSession,
  )

  async function login(email: string, password: string) {
    const result = await loginRequest(email, password)
    setSession(result)
    setSessionState(result)
  }

  function logout() {
    clearSession()
    setSessionState(undefined)
  }

  return (
    <AuthContext.Provider value={{ session, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }

  return context
}
