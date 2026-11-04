import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { account, pending, problem, renderAt, stubApi } from '../test/api.tsx'

describe('AdminPage', () => {
  it('shows the admin panel placeholder when the server allows it', async () => {
    stubApi({
      'GET /api/auth/me': account(['Admin', 'Bidder']),
      'GET /api/admin': Response.json({ area: 'admin' }),
    })
    renderAt('/yonetim')
    expect(await screen.findByText('Yönetim paneli')).toHaveAttribute('role', 'status')
  })

  it('shows "no permission" when the server refuses a bidder', async () => {
    stubApi({
      'GET /api/auth/me': account(['Bidder']),
      'GET /api/admin': problem(403, 'forbidden'),
    })
    renderAt('/yonetim')
    expect(await screen.findByRole('alert')).toHaveTextContent('Bu sayfaya erişim yetkiniz yok.')
  })

  it('follows the server, not the role list: a refused "admin" still sees no panel', async () => {
    stubApi({ 'GET /api/auth/me': account(['Admin']), 'GET /api/admin': problem(403, 'forbidden') })
    renderAt('/yonetim')
    expect(await screen.findByRole('alert')).toHaveTextContent('Bu sayfaya erişim yetkiniz yok.')
    expect(screen.queryByText('Yönetim paneli')).not.toBeInTheDocument()
  })

  it('sends a signed-out visitor to the sign-in page', async () => {
    stubApi()
    renderAt('/yonetim')
    expect(await screen.findByRole('heading', { name: 'Giriş yap' })).toBeInTheDocument()
  })

  it('shows loading while the server answers', async () => {
    stubApi({ 'GET /api/auth/me': account(['Admin']), 'GET /api/admin': pending })
    renderAt('/yonetim')
    expect(await screen.findByText('Yükleniyor…')).toHaveAttribute('role', 'status')
  })
})
