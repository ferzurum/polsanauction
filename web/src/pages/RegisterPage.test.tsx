import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { pending, problem, renderAt, stubApi } from '../test/api.tsx'

function fillIn(values: Partial<Record<string, string>> = {}) {
  const form = {
    Ad: 'Ayşe',
    Soyad: 'Yılmaz',
    'Kurumsal e-posta': 'ayse@polsanteknoloji.com.tr',
    Parola: 'on-karakter',
    'Parola (tekrar)': 'on-karakter',
    ...values,
  }
  for (const [label, value] of Object.entries(form)) {
    fireEvent.change(screen.getByLabelText(label), { target: { value } })
  }
  fireEvent.click(screen.getByRole('button', { name: 'Kayıt ol' }))
}

describe('RegisterPage', () => {
  it('shows an empty, fully labelled form', () => {
    stubApi()
    renderAt('/kayit')
    for (const label of ['Ad', 'Soyad', 'Kurumsal e-posta', 'Parola', 'Parola (tekrar)']) {
      expect(screen.getByLabelText(label)).toHaveValue('')
    }
  })

  it('sends the form with the anti-forgery header and shows "check your e-mail"', async () => {
    const calls = stubApi({ 'POST /api/auth/register': new Response(null, { status: 202 }) })
    renderAt('/kayit')
    fillIn()
    expect(await screen.findByRole('status')).toHaveTextContent('E-postanızı kontrol edin.')
    const post = calls.find((call) => call.path === '/api/auth/register')
    expect(post?.body).toEqual({
      firstName: 'Ayşe',
      lastName: 'Yılmaz',
      email: 'ayse@polsanteknoloji.com.tr',
      password: 'on-karakter',
      passwordConfirmation: 'on-karakter',
    })
    expect(post?.headers['X-XSRF-TOKEN']).toBe('csrf-token')
  })

  it('disables the button while sending', async () => {
    stubApi({ 'POST /api/auth/register': pending })
    renderAt('/kayit')
    fillIn()
    expect(await screen.findByRole('button', { name: 'Gönderiliyor…' })).toBeDisabled()
  })

  it('shows each field error under its field, tied to it for screen readers', async () => {
    stubApi({
      'POST /api/auth/register': problem(400, 'validation', {
        errors: {
          email: ['email.domain_not_allowed'],
          password: ['too_short'],
          passwordConfirmation: ['password.mismatch'],
        },
      }),
    })
    renderAt('/kayit')
    fillIn({ 'Kurumsal e-posta': 'ayse@gmail.com' })

    const email = screen.getByLabelText('Kurumsal e-posta')
    expect(
      await screen.findByText('Yalnızca kurumsal e-posta adresleriyle kayıt olunabilir.'),
    ).toHaveAttribute('role', 'alert')
    expect(email).toHaveAttribute('aria-invalid', 'true')
    expect(email).toHaveAccessibleDescription(
      'Yalnızca kurumsal e-posta adresleriyle kayıt olunabilir.',
    )
    expect(screen.getByLabelText('Parola')).toHaveAccessibleDescription(
      'Parola en az 10 karakter olmalı.',
    )
    expect(screen.getByLabelText('Parola (tekrar)')).toHaveAccessibleDescription(
      'Parolalar eşleşmiyor.',
    )
    expect(screen.getByLabelText('Ad')).toHaveAttribute('aria-invalid', 'false')
  })

  it('shows the rate-limit message', async () => {
    stubApi({ 'POST /api/auth/register': problem(429, 'rate_limited') })
    renderAt('/kayit')
    fillIn()
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Çok fazla deneme, lütfen sonra tekrar deneyin.',
    )
  })

  it('shows a server error', async () => {
    stubApi({ 'POST /api/auth/register': problem(500, 'error') })
    renderAt('/kayit')
    fillIn()
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Bir hata oluştu. Lütfen daha sonra tekrar deneyin.',
    )
  })

  // Enter, Tab order and focus are observed by hand, not here (plan 0002, A-2 d; VERIFY F-5).
  it('submits through the form submit event, not only the button click', async () => {
    stubApi({ 'POST /api/auth/register': new Response(null, { status: 202 }) })
    renderAt('/kayit')
    fireEvent.change(screen.getByLabelText('Ad'), { target: { value: 'Ayşe' } })
    fireEvent.submit(screen.getByRole('form', { name: 'Kayıt formu' }))
    expect(await screen.findByRole('status')).toHaveTextContent('E-postanızı kontrol edin.')
  })
})
