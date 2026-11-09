import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { confirmEmail } from '../api/auth.ts'
import type { ApiResult } from '../api/client.ts'
import { ResendConfirmation } from '../components/ResendConfirmation.tsx'
import { messages } from '../messages.ts'

type State = 'confirming' | 'confirmed' | 'invalid' | 'error'

/** The page the confirmation e-mail links to: /dogrula?hesap=…&kod=… (spec 0002, AC-5). */
export function ConfirmEmailPage() {
  const [params] = useSearchParams()
  const accountId = params.get('hesap')
  const token = params.get('kod')
  const [state, setState] = useState<State>(accountId && token ? 'confirming' : 'invalid')
  // A link is single-use: send it once per link, even when StrictMode runs the effect twice in development
  // (the second request would answer "already used").
  const request = useRef<{ link: string; result: Promise<ApiResult<undefined>> } | undefined>(
    undefined,
  )

  useEffect(() => {
    if (!accountId || !token) {
      return
    }
    const link = `${accountId}|${token}`
    if (request.current?.link !== link) {
      request.current = { link, result: confirmEmail(accountId, token) }
    }
    let active = true
    request.current.result
      .then((result) => {
        if (active) {
          setState(result.ok ? 'confirmed' : result.problem.status === 400 ? 'invalid' : 'error')
        }
      })
      .catch(() => {
        if (active) {
          setState('error')
        }
      })
    return () => {
      active = false
    }
  }, [accountId, token])

  return (
    <section>
      <h2>E-posta doğrulama</h2>
      {state === 'confirming' && <p role="status">{messages.confirming}</p>}
      {state === 'confirmed' && (
        <>
          <p role="status">{messages.confirmed}</p>
          <Link to="/giris">Giriş yap</Link>
        </>
      )}
      {state === 'invalid' && (
        <>
          <p role="alert">{messages.confirmationInvalid}</p>
          <ResendConfirmation />
        </>
      )}
      {state === 'error' && <p role="alert">{messages.serverError}</p>}
    </section>
  )
}
