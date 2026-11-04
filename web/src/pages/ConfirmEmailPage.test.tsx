import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { pending, problem, renderAt, stubApi } from '../test/api.tsx'

const link = '/dogrula?hesap=0190f0e0-0000-7000-8000-000000000001&kod=abc%2Bdef'

describe('ConfirmEmailPage', () => {
  it('shows "confirming" while the link is checked', () => {
    stubApi({ 'POST /api/auth/confirm-email': pending })
    renderAt(link)
    expect(screen.getByRole('status')).toHaveTextContent('E-posta adresiniz doğrulanıyor…')
  })

  it('sends the account and the decoded code, then shows success with a sign-in link', async () => {
    const calls = stubApi({ 'POST /api/auth/confirm-email': new Response(null, { status: 204 }) })
    renderAt(link)
    expect(await screen.findByText('E-postanız doğrulandı, giriş yapabilirsiniz.')).toHaveAttribute(
      'role',
      'status',
    )
    expect(
      within(screen.getByRole('main')).getByRole('link', { name: 'Giriş yap' }),
    ).toBeInTheDocument()
    expect(calls.find((call) => call.path === '/api/auth/confirm-email')?.body).toEqual({
      accountId: '0190f0e0-0000-7000-8000-000000000001',
      token: 'abc+def',
    })
  })

  it('explains an invalid, expired or used link and offers to resend', async () => {
    const calls = stubApi({
      'POST /api/auth/confirm-email': problem(400, 'confirmation.invalid'),
      'POST /api/auth/resend-confirmation': new Response(null, { status: 202 }),
    })
    renderAt(link)
    expect(
      await screen.findByText('Bu bağlantı geçersiz, süresi dolmuş ya da daha önce kullanılmış.'),
    ).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('E-posta'), {
      target: { value: 'ayse@polsanteknoloji.com.tr' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrulama e-postasını yeniden gönder' }))

    expect(
      await screen.findByText(
        'Yeni bir doğrulama bağlantısı gönderildi. E-postanızı kontrol edin.',
      ),
    ).toBeInTheDocument()
    expect(calls.find((call) => call.path === '/api/auth/resend-confirmation')?.body).toEqual({
      email: 'ayse@polsanteknoloji.com.tr',
    })
  })

  it('treats a link without code as invalid without calling the API', () => {
    const calls = stubApi()
    renderAt('/dogrula?hesap=0190f0e0-0000-7000-8000-000000000001')
    expect(
      screen.getByText('Bu bağlantı geçersiz, süresi dolmuş ya da daha önce kullanılmış.'),
    ).toBeInTheDocument()
    expect(calls.some((call) => call.path === '/api/auth/confirm-email')).toBe(false)
  })

  it('shows the rate-limit message when resending too often', async () => {
    stubApi({
      'POST /api/auth/confirm-email': problem(400, 'confirmation.invalid'),
      'POST /api/auth/resend-confirmation': problem(429, 'rate_limited'),
    })
    renderAt(link)
    fireEvent.click(
      await screen.findByRole('button', { name: 'Doğrulama e-postasını yeniden gönder' }),
    )
    expect(
      await screen.findByText('Çok fazla deneme, lütfen sonra tekrar deneyin.'),
    ).toBeInTheDocument()
  })

  it('shows a server error', async () => {
    stubApi({ 'POST /api/auth/confirm-email': problem(500, 'error') })
    renderAt(link)
    expect(
      await screen.findByText('Bir hata oluştu. Lütfen daha sonra tekrar deneyin.'),
    ).toBeInTheDocument()
  })
})
