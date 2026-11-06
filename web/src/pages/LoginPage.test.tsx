import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { account, pending, problem, renderAt, signedOut, stubApi } from '../test/api.tsx'

function signIn(email = 'ayse@polsanteknoloji.com.tr', password = 'on-karakter') {
  fireEvent.change(screen.getByLabelText('E-posta'), { target: { value: email } })
  fireEvent.change(screen.getByLabelText('Parola'), { target: { value: password } })
  fireEvent.click(screen.getByRole('button', { name: 'Giriş yap' }))
}

describe('LoginPage', () => {
  it('shows an empty, labelled form', () => {
    stubApi()
    renderAt('/giris')
    expect(screen.getByLabelText('E-posta')).toHaveValue('')
    expect(screen.getByLabelText('Parola')).toHaveAttribute('autocomplete', 'current-password')
  })

  it('disables the button while signing in', async () => {
    stubApi({ 'POST /api/auth/login': pending })
    renderAt('/giris')
    signIn()
    expect(await screen.findByRole('button', { name: 'Gönderiliyor…' })).toBeDisabled()
  })

  it('signs in, shows the person in the top bar and goes home', async () => {
    let signedIn = false
    stubApi({
      'POST /api/auth/login': () => {
        signedIn = true
        return new Response(null, { status: 204 })
      },
      'GET /api/auth/me': () => (signedIn ? account() : signedOut()),
    })
    renderAt('/giris')
    signIn()
    expect(await screen.findByText('Ayşe Yılmaz')).toBeInTheDocument()
    expect(await screen.findByText('Sistem çalışıyor.')).toBeInTheDocument()
  })

  it('shows one generic message for wrong credentials', async () => {
    stubApi({ 'POST /api/auth/login': problem(401, 'auth.invalid_credentials') })
    renderAt('/giris')
    signIn()
    expect(await screen.findByRole('alert')).toHaveTextContent('E-posta adresi veya parola hatalı.')
  })

  it('offers to resend the confirmation for an unconfirmed account, prefilled', async () => {
    stubApi({ 'POST /api/auth/login': problem(403, 'auth.email_not_confirmed') })
    renderAt('/giris')
    signIn('ayse@polsanteknoloji.com.tr')
    expect(await screen.findByText('E-posta adresiniz henüz doğrulanmadı.')).toHaveAttribute(
      'role',
      'alert',
    )
    expect(
      screen.getByRole('button', { name: 'Doğrulama e-postasını yeniden gönder' }),
    ).toBeInTheDocument()
    expect(screen.getAllByLabelText('E-posta')[1]).toHaveValue('ayse@polsanteknoloji.com.tr')
  })

  it('shows until when the account is locked, in Istanbul time', async () => {
    stubApi({
      'POST /api/auth/login': problem(403, 'auth.locked_out', {
        lockedUntil: '2026-01-05T11:35:00+00:00',
      }),
    })
    renderAt('/giris')
    signIn()
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Çok fazla hatalı deneme. Hesabınız geçici olarak kilitlendi. Kilit açılış saati: 14:35.',
    )
  })

  it('shows a server error', async () => {
    stubApi({ 'POST /api/auth/login': problem(500, 'error') })
    renderAt('/giris')
    signIn()
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Bir hata oluştu. Lütfen daha sonra tekrar deneyin.',
    )
  })
})
