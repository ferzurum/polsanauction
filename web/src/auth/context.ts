import { createContext, useContext } from 'react'
import type { Account } from '../api/auth.ts'

export interface AuthState {
  /** undefined while loading, null when signed out. */
  account: Account | null | undefined
  refresh: () => Promise<void>
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthState | undefined>(undefined)

export function useAuth(): AuthState {
  const state = useContext(AuthContext)
  if (state === undefined) {
    throw new Error('useAuth must be used inside AuthProvider')
  }
  return state
}

/** Only for showing the admin link; the server decides access (docs/security.md). */
export const isAdmin = (account: Account | null | undefined) =>
  account?.roles.includes('Admin') ?? false
