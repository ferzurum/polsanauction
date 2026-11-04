import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { currentAccount, logout, type Account } from '../api/auth.ts'
import { AuthContext } from './context.ts'

async function loadAccount(): Promise<Account | null> {
  try {
    const result = await currentAccount()
    return result.ok ? result.data : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [account, setAccount] = useState<Account | null | undefined>(undefined)

  useEffect(() => {
    let active = true
    void loadAccount().then((loaded) => {
      if (active) {
        setAccount(loaded)
      }
    })
    return () => {
      active = false
    }
  }, [])

  const refresh = useCallback(async () => setAccount(await loadAccount()), [])

  const signOut = useCallback(async () => {
    await logout()
    setAccount(null)
  }, [])

  const value = useMemo(() => ({ account, refresh, signOut }), [account, refresh, signOut])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
