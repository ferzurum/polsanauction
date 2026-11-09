import { beforeEach, describe, expect, it } from 'vitest'
import { problem, stubApi } from '../test/api.tsx'
import { api } from './client.ts'

/** Hands out "token-1", "token-2", … so a test can tell a cached token from a fresh one. */
function tokens() {
  let issued = 0
  return () => Response.json({ token: `token-${String(++issued)}` })
}

describe('api client — anti-forgery token', () => {
  beforeEach(() => {
    stubApi()
  })

  it('fetches a fresh token and retries once when the cached one is rejected', async () => {
    const calls = stubApi({
      'GET /api/auth/antiforgery': tokens(),
      'POST /api/auth/login': ({ headers }) =>
        headers['X-XSRF-TOKEN'] === 'token-2'
          ? new Response(null, { status: 204 })
          : problem(400, 'csrf.invalid'),
    })

    const result = await api('/api/auth/login', { method: 'POST', body: {} })

    expect(result.ok).toBe(true)
    const logins = calls.filter((call) => call.path === '/api/auth/login')
    expect(logins.map((call) => call.headers['X-XSRF-TOKEN'])).toEqual(['token-1', 'token-2'])
  })

  it('retries only once', async () => {
    const calls = stubApi({
      'GET /api/auth/antiforgery': tokens(),
      'POST /api/auth/login': problem(400, 'csrf.invalid'),
    })

    const result = await api('/api/auth/login', { method: 'POST', body: {} })

    expect(result).toEqual({ ok: false, problem: { status: 400, code: 'csrf.invalid' } })
    expect(calls.filter((call) => call.path === '/api/auth/login')).toHaveLength(2)
  })

  it('forgets the token when the session has ended (401)', async () => {
    const calls = stubApi({
      'GET /api/auth/antiforgery': tokens(),
      'POST /api/auth/resend-confirmation': new Response(null, { status: 202 }),
      'GET /api/admin': problem(401, 'unauthorized'),
    })
    await api('/api/auth/resend-confirmation', { method: 'POST', body: {} })

    await api('/api/admin')
    await api('/api/auth/resend-confirmation', { method: 'POST', body: {} })

    const sent = calls.filter((call) => call.path === '/api/auth/resend-confirmation')
    expect(sent.map((call) => call.headers['X-XSRF-TOKEN'])).toEqual(['token-1', 'token-2'])
  })
})
