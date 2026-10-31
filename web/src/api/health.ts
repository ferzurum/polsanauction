export type HealthStatus = 'healthy' | 'unhealthy'

/** Asks the backend whether the application and its database are up (spec 0001, R-2). */
export async function fetchHealth(signal?: AbortSignal): Promise<HealthStatus> {
  const response = await fetch('/api/health', { signal })
  if (!response.ok) {
    return 'unhealthy'
  }
  const body: unknown = await response.json()
  return isHealthyBody(body) ? 'healthy' : 'unhealthy'
}

function isHealthyBody(body: unknown): boolean {
  return typeof body === 'object' && body !== null && 'status' in body && body.status === 'Healthy'
}
