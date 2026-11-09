/** A ProblemDetails answer from the API (docs/conventions.md, Error handling). */
export interface ApiProblem {
  status: number
  code?: string
  errors?: Record<string, string[]>
  lockedUntil?: string
}

export type ApiResult<T> =
  { ok: true; status: number; data: T } | { ok: false; problem: ApiProblem }

const antiforgeryHeader = 'X-XSRF-TOKEN'
let antiforgeryToken: string | undefined

/** The token is bound to the signed-in user: forget it after sign-in and sign-out (docs/security.md). */
export function resetAntiforgeryToken() {
  antiforgeryToken = undefined
}

async function getAntiforgeryToken(): Promise<string> {
  if (antiforgeryToken === undefined) {
    const response = await fetch('/api/auth/antiforgery', { credentials: 'same-origin' })
    const body = (await response.json()) as { token: string }
    antiforgeryToken = body.token
  }
  return antiforgeryToken
}

/** Calls the API; state-changing requests carry the anti-forgery header. Never throws for HTTP errors. */
export async function api<T = undefined>(
  path: string,
  options: { method?: 'GET' | 'POST'; body?: unknown } = {},
): Promise<ApiResult<T>> {
  const first = await send<T>(path, options)
  if (first.ok) {
    return first
  }
  if (first.problem.status === 401) {
    // The session is gone (idle limit, sign-out in another tab): the cached token belonged to it.
    resetAntiforgeryToken()
  }
  // A token of a session that has ended (or of another tab's sign-in) is rejected: fetch a fresh one, retry once.
  if (first.problem.code === 'csrf.invalid') {
    resetAntiforgeryToken()
    return send<T>(path, options)
  }
  return first
}

async function send<T>(
  path: string,
  options: { method?: 'GET' | 'POST'; body?: unknown },
): Promise<ApiResult<T>> {
  const method = options.method ?? 'GET'
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (method !== 'GET') {
    headers[antiforgeryHeader] = await getAntiforgeryToken()
  }
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const response = await fetch(path, {
    method,
    headers,
    credentials: 'same-origin',
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  })
  const text = await response.text()
  const json: unknown = text.length > 0 ? JSON.parse(text) : undefined

  if (response.ok) {
    return { ok: true, status: response.status, data: json as T }
  }
  const problem = (json ?? {}) as Partial<ApiProblem>
  return { ok: false, problem: { ...problem, status: response.status } }
}
