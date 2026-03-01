import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'
import type { ReactNode } from 'react'

interface Props {
  children: ReactNode
  requireAdmin?: boolean
}

export function ProtectedRoute({ children, requireAdmin = false }: Props) {
  const { token, role } = useAuth()
  const location = useLocation()

  if (!token) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (requireAdmin && role !== 'Admin') {
    return <Navigate to="/keys" replace />
  }

  return <>{children}</>
}
