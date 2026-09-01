import type { ApiError } from '../api/apiError'

interface ErrorPanelProps {
  error: ApiError
}

export function ErrorPanel({ error }: ErrorPanelProps) {
  const fieldErrors = Object.entries(error.errors ?? {})

  return (
    <section className="panel panel--error" role="alert" aria-labelledby="error-heading">
      <h2 id="error-heading">{error.title ?? `Request failed (${error.status})`}</h2>

      {error.detail && <p className="panel__detail">{error.detail}</p>}

      {fieldErrors.length > 0 && (
        <dl className="panel__field-errors">
          {fieldErrors.map(([field, messages]) => (
            <div key={field}>
              <dt>{field}</dt>
              <dd>
                <ul className="panel__list">
                  {messages.map((message) => (
                    <li key={message}>{message}</li>
                  ))}
                </ul>
              </dd>
            </div>
          ))}
        </dl>
      )}

      {error.traceId && <p className="panel__trace">Trace ID: {error.traceId}</p>}
    </section>
  )
}
