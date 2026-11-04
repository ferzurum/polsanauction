import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from './App.tsx'

function stubFetch(implementation: () => Promise<Response>) {
  vi.stubGlobal('fetch', vi.fn(implementation))
}

describe('App — sistem durumu', () => {
  it('shows the application name', () => {
    stubFetch(() => new Promise<Response>(() => {}))
    render(<App />)
    expect(screen.getByRole('heading', { level: 1, name: 'Polsan Auction' })).toBeInTheDocument()
  })

  it('shows the loading state while the check is pending', () => {
    stubFetch(() => new Promise<Response>(() => {}))
    render(<App />)
    expect(screen.getByRole('status')).toHaveTextContent('Sistem durumu kontrol ediliyor…')
  })

  it('shows the healthy state when the backend reports Healthy', async () => {
    stubFetch(() => Promise.resolve(Response.json({ status: 'Healthy' })))
    render(<App />)
    expect(await screen.findByText('Sistem çalışıyor.')).toHaveAttribute('role', 'status')
  })

  it('shows the error state when the backend reports Unhealthy', async () => {
    stubFetch(() => Promise.resolve(Response.json({ status: 'Unhealthy' }, { status: 503 })))
    render(<App />)
    expect(
      await screen.findByText('Sistem şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.'),
    ).toHaveAttribute('role', 'status')
  })

  it('shows the error state when a 200 response does not report Healthy', async () => {
    stubFetch(() => Promise.resolve(Response.json({ status: 'Unhealthy' })))
    render(<App />)
    expect(
      await screen.findByText('Sistem şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.'),
    ).toHaveAttribute('role', 'status')
  })

  it('shows the error state when the backend cannot be reached', async () => {
    stubFetch(() => Promise.reject(new TypeError('Failed to fetch')))
    render(<App />)
    expect(
      await screen.findByText('Sistem şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.'),
    ).toBeInTheDocument()
  })
})
