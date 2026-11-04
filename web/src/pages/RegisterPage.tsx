import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { register, type SignUpForm } from '../api/auth.ts'
import { TextField } from '../components/TextField.tsx'
import { fieldError, isRateLimited, messages } from '../messages.ts'

type State =
  | { kind: 'idle' | 'submitting' | 'success' | 'limited' | 'error' }
  | { kind: 'invalid'; errors: Record<string, string[]> }

const empty: SignUpForm = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  passwordConfirmation: '',
}

/** Sign-up (spec 0002, R-1…R-3). The result is the same whether or not the address already has an account. */
export function RegisterPage() {
  const [form, setForm] = useState(empty)
  const [state, setState] = useState<State>({ kind: 'idle' })

  const errors = state.kind === 'invalid' ? state.errors : {}
  const field = (name: keyof SignUpForm) => ({
    value: form[name],
    onChange: (event: { target: { value: string } }) =>
      setForm({ ...form, [name]: event.target.value }),
    error: fieldError(name, errors[name]),
  })

  async function submit(event: FormEvent) {
    event.preventDefault()
    setState({ kind: 'submitting' })
    try {
      const result = await register(form)
      if (result.ok) {
        setState({ kind: 'success' })
      } else if (result.problem.code === 'validation') {
        setState({ kind: 'invalid', errors: result.problem.errors ?? {} })
      } else {
        setState({ kind: isRateLimited(result.problem) ? 'limited' : 'error' })
      }
    } catch {
      setState({ kind: 'error' })
    }
  }

  if (state.kind === 'success') {
    return (
      <section>
        <h2>Kayıt ol</h2>
        <p role="status">{messages.checkEmail}</p>
        <Link to="/giris">Giriş ekranına git</Link>
      </section>
    )
  }

  return (
    <section>
      <h2>Kayıt ol</h2>
      <form onSubmit={submit} noValidate aria-label="Kayıt formu">
        <TextField label="Ad" autoComplete="given-name" {...field('firstName')} />
        <TextField label="Soyad" autoComplete="family-name" {...field('lastName')} />
        <TextField label="Kurumsal e-posta" type="email" autoComplete="email" {...field('email')} />
        <TextField
          label="Parola"
          type="password"
          autoComplete="new-password"
          {...field('password')}
        />
        <TextField
          label="Parola (tekrar)"
          type="password"
          autoComplete="new-password"
          {...field('passwordConfirmation')}
        />
        <button type="submit" disabled={state.kind === 'submitting'}>
          {state.kind === 'submitting' ? 'Gönderiliyor…' : 'Kayıt ol'}
        </button>
        {state.kind === 'limited' && <p role="alert">{messages.tooManyAttempts}</p>}
        {state.kind === 'error' && <p role="alert">{messages.serverError}</p>}
      </form>
      <p>
        Zaten hesabınız var mı? <Link to="/giris">Giriş yapın</Link>
      </p>
    </section>
  )
}
