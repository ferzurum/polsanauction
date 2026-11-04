import { fireEvent, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { account, pending, renderAt, stubApi } from './test/api.tsx'

const unavailable = 'Sistem şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.'

describe('App — sistem durumu', () => {
  it('shows the application name', () => {
    stubApi({ 'GET /api/health': pending })
    renderAt('/')
    expect(screen.getByRole('heading', { level: 1, name: 'Polsan Auction' })).toBeInTheDocument()
  })

  it('shows the loading state while the check is pending', () => {
    stubApi({ 'GET /api/health': pending })
    renderAt('/')
    expect(screen.getByRole('status')).toHaveTextContent('Sistem durumu kontrol ediliyor…')
  })

  it('shows the healthy state when the backend reports Healthy', async () => {
    stubApi({ 'GET /api/health': Response.json({ status: 'Healthy' }) })
    renderAt('/')
    expect(await screen.findByText('Sistem çalışıyor.')).toHaveAttribute('role', 'status')
  })

  it('shows the error state when the backend reports Unhealthy', async () => {
    stubApi({ 'GET /api/health': Response.json({ status: 'Unhealthy' }, { status: 503 }) })
    renderAt('/')
    expect(await screen.findByText(unavailable)).toHaveAttribute('role', 'status')
  })

  it('shows the error state when a 200 response does not report Healthy', async () => {
    stubApi({ 'GET /api/health': Response.json({ status: 'Unhealthy' }) })
    renderAt('/')
    expect(await screen.findByText(unavailable)).toHaveAttribute('role', 'status')
  })

  it('shows the error state when the backend cannot be reached', async () => {
    stubApi({ 'GET /api/health': () => Promise.reject(new TypeError('Failed to fetch')) })
    renderAt('/')
    expect(await screen.findByText(unavailable)).toBeInTheDocument()
  })
})

describe('App — üst çubuk', () => {
  it('offers sign-in and sign-up when signed out', async () => {
    stubApi()
    renderAt('/')
    const nav = await screen.findByRole('navigation', { name: 'Hesap' })
    expect(await within(nav).findByRole('link', { name: 'Giriş yap' })).toBeInTheDocument()
    expect(within(nav).getByRole('link', { name: 'Kayıt ol' })).toBeInTheDocument()
  })

  it('shows the name and sign-out, but no admin link, for a bidder', async () => {
    stubApi({ 'GET /api/auth/me': account(['Bidder']) })
    renderAt('/')
    expect(await screen.findByText('Ayşe Yılmaz')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Çıkış' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Yönetim' })).not.toBeInTheDocument()
  })

  it('shows the admin link for an admin', async () => {
    stubApi({ 'GET /api/auth/me': account(['Admin', 'Bidder']) })
    renderAt('/')
    expect(await screen.findByRole('link', { name: 'Yönetim' })).toHaveAttribute('href', '/yonetim')
  })

  it('signs out and goes to the sign-in page', async () => {
    const calls = stubApi({
      'GET /api/auth/me': account(),
      'POST /api/auth/logout': new Response(null, { status: 204 }),
    })
    renderAt('/')
    fireEvent.click(await screen.findByRole('button', { name: 'Çıkış' }))
    expect(await screen.findByRole('heading', { name: 'Giriş yap' })).toBeInTheDocument()
    expect(calls.some((call) => call.method === 'POST' && call.path === '/api/auth/logout')).toBe(
      true,
    )
  })
})
