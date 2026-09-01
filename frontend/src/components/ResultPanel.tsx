import type { AnalyseFaultResponse } from '../types/faultAssessment'

interface ResultPanelProps {
  result: AnalyseFaultResponse
}

function StringList({ items }: { items: string[] }) {
  if (items.length === 0) {
    return <p className="panel__empty">None provided.</p>
  }

  return (
    <ul className="panel__list">
      {items.map((item) => (
        <li key={item}>{item}</li>
      ))}
    </ul>
  )
}

export function ResultPanel({ result }: ResultPanelProps) {
  return (
    <section className="panel panel--result" aria-labelledby="result-heading">
      <h2 id="result-heading">Triage result</h2>

      {result.safetyWarning && (
        <p className="panel__safety" role="alert">
          {result.safetyWarning}
        </p>
      )}

      <dl className="panel__summary">
        <div>
          <dt>Customer concern</dt>
          <dd>{result.customerConcern}</dd>
        </div>
        <div>
          <dt>Vehicle system</dt>
          <dd>{result.vehicleSystem}</dd>
        </div>
        <div>
          <dt>Urgency</dt>
          <dd>{result.urgency}</dd>
        </div>
      </dl>

      <h3>Symptoms</h3>
      <StringList items={result.symptoms} />

      <h3>Workshop checks</h3>
      <StringList items={result.workshopChecks} />

      <h3>Clarifying questions</h3>
      <StringList items={result.clarifyingQuestions} />
    </section>
  )
}
