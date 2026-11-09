import { useState, type FormEvent } from 'react'
import { resendConfirmation } from '../api/auth.ts'
import { isRateLimited, messages } from '../messages.ts'
import { TextField } from './TextField.tsx'

type State = 'idle' | 'sending' | 'sent' | 'limited' | 'error'

/** "Send the confirmation e-mail again" — offered on the login and confirmation pages (spec 0002, R-4). */
export function ResendConfirmation({ initialEmail = '' }: { initialEmail?: string }) {
  const [email, setEmail] = useState(initialEmail)
  const [state, setState] = useState<State>('idle')

  async function submit(event: FormEvent) {
    event.preventDefault()
    setState('sending')
    try {
      const result = await resendConfirmation(email)
      setState(result.ok ? 'sent' : isRateLimited(result.problem) ? 'limited' : 'error')
    } catch {
      setState('error')
    }
  }

  return (
    <form onSubmit={submit} aria-label="Doğrulama e-postasını yeniden gönder">
      <TextField
        label="E-posta"
        type="email"
        autoComplete="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <button type="submit" disabled={state === 'sending'}>
        {state === 'sending' ? 'Gönderiliyor…' : 'Doğrulama e-postasını yeniden gönder'}
      </button>
      {state === 'sent' && <p role="status">{messages.confirmationSent}</p>}
      {state === 'limited' && <p role="alert">{messages.tooManyAttempts}</p>}
      {state === 'error' && <p role="alert">{messages.serverError}</p>}
    </form>
  )
}
