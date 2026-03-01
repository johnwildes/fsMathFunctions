import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react'

const TOKEN_KEY = 'portal_token'

interface JwtPayload {
  sub: string
  email: string
  role: string
  exp: number
}

function parseToken(token: string): JwtPayload | null {
  try {
    const payload = token.split('.')[1]
    return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as JwtPayload
  } catch {
    return null
  }
}

interface AuthContextValue {
  token: string | null
  email: string | null
  role: string | null
  setToken: (token: string | null) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenState] = useState<string | null>(
    () => localStorage.getItem(TOKEN_KEY)
  )

  const setToken = useCallback((t: string | null) => {
    setTokenState(t)
    if (t) {
      localStorage.setItem(TOKEN_KEY, t)
    } else {
      localStorage.removeItem(TOKEN_KEY)
    }
  }, [])

  const logout = useCallback(() => setToken(null), [setToken])

  const payload = token ? parseToken(token) : null

  // Auto-logout if token is expired
  if (payload && payload.exp * 1000 < Date.now()) {
    localStorage.removeItem(TOKEN_KEY)
  }

  return (
    <AuthContext.Provider
      value={{
        token: payload && payload.exp * 1000 > Date.now() ? token : null,
        email: payload?.email ?? null,
        role: payload?.role ?? null,
        setToken,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
