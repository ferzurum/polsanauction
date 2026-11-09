import { useEffect, useState } from 'react'
import { Navigate } from 'react-router'
import { adminArea } from '../api/auth.ts'
import { useAuth } from '../auth/context.ts'
import { messages } from '../messages.ts'

type Answer = 'allowed' | 'forbidden' | 'signedOut' | 'error'

/** The admin area placeholder (spec 0002, AC-9). Access is decided by the server's answer, not by the role list. */
export function AdminPage() {
  const { account } = useAuth()
  const [answer, setAnswer] = useState<Answer | undefined>(undefined)

  useEffect(() => {
    if (!account) {
      return
    }
    let active = true
    adminArea()
      .then((result) => {
        if (!active) {
          return
        }
        if (result.ok) {
          setAnswer('allowed')
        } else {
          const { status } = result.problem
          setAnswer(status === 403 ? 'forbidden' : status === 401 ? 'signedOut' : 'error')
        }
      })
      .catch(() => {
        if (active) {
          setAnswer('error')
        }
      })
    return () => {
      active = false
    }
  }, [account])

  if (account === null || answer === 'signedOut') {
    return <Navigate to="/giris" replace />
  }

  return (
    <section>
      <h2>Yönetim</h2>
      {answer === undefined && <p role="status">Yükleniyor…</p>}
      {answer === 'allowed' && <p role="status">{messages.adminPanel}</p>}
      {answer === 'forbidden' && <p role="alert">{messages.noPermission}</p>}
      {answer === 'error' && <p role="alert">{messages.serverError}</p>}
    </section>
  )
}
