import { useEffect, useState } from 'react'
import { fetchHealth, type HealthStatus } from '../api/health.ts'

type ViewState = 'loading' | HealthStatus

const statusMessages: Record<ViewState, string> = {
  loading: 'Sistem durumu kontrol ediliyor…',
  healthy: 'Sistem çalışıyor.',
  unhealthy: 'Sistem şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.',
}

/** The home page: system status (spec 0001). */
export function HomePage() {
  const [state, setState] = useState<ViewState>('loading')

  useEffect(() => {
    const controller = new AbortController()
    fetchHealth(controller.signal)
      .then(setState)
      .catch(() => {
        if (!controller.signal.aborted) {
          setState('unhealthy')
        }
      })
    return () => controller.abort()
  }, [])

  return (
    // The status is stated in words, never by colour alone (spec 0001, Accessibility).
    <p role="status" aria-live="polite" data-state={state}>
      {statusMessages[state]}
    </p>
  )
}
