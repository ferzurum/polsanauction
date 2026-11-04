import { useEffect, useState } from 'react'
import { fetchHealth, type HealthStatus } from './api/health.ts'

type ViewState = 'loading' | HealthStatus

const messages: Record<ViewState, string> = {
  loading: 'Sistem durumu kontrol ediliyor…',
  healthy: 'Sistem çalışıyor.',
  unhealthy: 'Sistem şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.',
}

export default function App() {
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
    <main>
      <h1>Polsan Auction</h1>
      {/* The status is stated in words, never by colour alone (spec 0001, Accessibility). */}
      <p role="status" aria-live="polite" data-state={state}>
        {messages[state]}
      </p>
    </main>
  )
}
