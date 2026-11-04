import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { login } from '../api/auth.ts'
import { useAuth } from '../auth/context.ts'
import { ResendConfirmation } from '../components/ResendConfirmation.tsx'
import { TextField } from '../components/TextField.tsx'
import { lockedMessage, messages } from '../messages.ts'

type State =
  | { kind: 'idle' | 'submitting' | 'invalid' | 'unconfirmed' | 'error' }
  | { kind: 'locked'; until?: string }

/** Sign-in (spec 0002, R-5, R-6, AC-6, AC-7). */
export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [state, setState] = useState<State>({ kind: 'idle' })
  const { refresh } = useAuth()
  const navigate = useNavigate()

  async function submit(event: FormEvent) {
    event.preventDefault()
    setState({ kind: 'submitting' })
    try {
      const result = await login(email, password)
      if (result.ok) {
        await refresh()
        void navigate('/')
        return
      }
      switch (result.problem.code) {
        case 'auth.invalid_credentials':
          setState({ kind: 'invalid' })
          break
        case 'auth.email_not_confirmed':
          setState({ kind: 'unconfirmed' })
          break
        case 'auth.locked_out':
          setState({ kind: 'locked', until: result.problem.lockedUntil })
          break
        default:
          setState({ kind: 'error' })
      }
    } catch {
      setState({ kind: 'error' })
    }
  }

  return (
    <section>
      <h2>Giriş yap</h2>
      <form onSubmit={submit} noValidate aria-label="Giriş formu">
        <TextField
          label="E-posta"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
        <TextField
          label="Parola"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <button type="submit" disabled={state.kind === 'submitting'}>
          {state.kind === 'submitting' ? 'Gönderiliyor…' : 'Giriş yap'}
        </button>
        {state.kind === 'invalid' && <p role="alert">{messages.invalidCredentials}</p>}
        {state.kind === 'locked' && <p role="alert">{lockedMessage(state.until)}</p>}
        {state.kind === 'error' && <p role="alert">{messages.serverError}</p>}
      </form>
      {state.kind === 'unconfirmed' && (
        <>
          <p role="alert">{messages.emailNotConfirmed}</p>
          <ResendConfirmation initialEmail={email} />
        </>
      )}
      <p>
        Hesabınız yok mu? <Link to="/kayit">Kayıt olun</Link>
      </p>
    </section>
  )
}
