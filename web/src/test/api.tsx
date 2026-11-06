import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { vi } from 'vitest'
import App from '../App.tsx'
import { resetAntiforgeryToken } from '../api/client.ts'

type Handler = (request: {
  method: string
  body: unknown
  headers: Record<string, string>
}) => Response | Promise<Response>

export const problem = (status: number, code: string, extra: Record<string, unknown> = {}) =>
  Response.json({ status, code, ...extra }, { status })

export const signedOut = () => problem(401, 'unauthorized')

export const account = (roles: string[] = ['Bidder']) =>
  Response.json({
    id: '1',
    firstName: 'Ayşe',
    lastName: 'Yılmaz',
    email: 'ayse@polsanteknoloji.com.tr',
    roles,
  })

/**
 * Stubs fetch per "METHOD /path". Unlisted calls: the anti-forgery token, a signed-out /me and a healthy
 * /api/health, so a test only names what it is about. Every call is recorded for assertions.
 */
export function stubApi(routes: Record<string, Handler | Response> = {}) {
  resetAntiforgeryToken()
  const calls: { method: string; path: string; body: unknown; headers: Record<string, string> }[] =
    []
  const fetchMock = vi.fn(async (input: string, init: RequestInit = {}) => {
    const method = init.method ?? 'GET'
    const path = input.split('?')[0]
    const body: unknown = typeof init.body === 'string' ? JSON.parse(init.body) : undefined
    const headers = (init.headers ?? {}) as Record<string, string>
    calls.push({ method, path, body, headers })
    const route = routes[`${method} ${path}`]
    if (route !== undefined) {
      return typeof route === 'function' ? route({ method, body, headers }) : route.clone()
    }
    if (path === '/api/auth/antiforgery') {
      return Response.json({ token: 'csrf-token' })
    }
    if (path === '/api/auth/me') {
      return signedOut()
    }
    if (path === '/api/health') {
      return Response.json({ status: 'Healthy' })
    }
    return problem(404, 'not_found')
  })
  vi.stubGlobal('fetch', fetchMock)
  return calls
}

/** A promise that never settles: keeps a request "in flight" to observe loading states. */
export const pending = () => new Promise<Response>(() => {})

export function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}
