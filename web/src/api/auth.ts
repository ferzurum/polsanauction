import { api, resetAntiforgeryToken } from './client.ts'

export interface Account {
  id: string
  firstName: string
  lastName: string
  email: string
  roles: string[]
}

export interface SignUpForm {
  firstName: string
  lastName: string
  email: string
  password: string
  passwordConfirmation: string
}

export const register = (form: SignUpForm) =>
  api('/api/auth/register', { method: 'POST', body: form })

export const confirmEmail = (accountId: string, token: string) =>
  api('/api/auth/confirm-email', { method: 'POST', body: { accountId, token } })

export const resendConfirmation = (email: string) =>
  api('/api/auth/resend-confirmation', { method: 'POST', body: { email } })

export async function login(email: string, password: string) {
  const result = await api('/api/auth/login', { method: 'POST', body: { email, password } })
  resetAntiforgeryToken()
  return result
}

export async function logout() {
  const result = await api('/api/auth/logout', { method: 'POST' })
  resetAntiforgeryToken()
  return result
}

export const currentAccount = () => api<Account>('/api/auth/me')

export const adminArea = () => api<{ area: string }>('/api/admin')
