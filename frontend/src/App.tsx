import { useState } from 'react'
import { ApiError } from './api/apiError'
import { analyseFault } from './api/faultAssessment'
import { Disclaimer } from './components/Disclaimer'
import { ErrorPanel } from './components/ErrorPanel'
import { FaultForm } from './components/FaultForm'
import { ResultPanel } from './components/ResultPanel'
import type { AnalyseFaultResponse } from './types/faultAssessment'
import './App.css'

function App() {
  const [description, setDescription] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [result, setResult] = useState<AnalyseFaultResponse | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  async function handleSubmit() {
    setIsLoading(true)
    setResult(null)
    setError(null)

    try {
      setResult(await analyseFault(description))
    } catch (caught) {
      if (caught instanceof ApiError) {
        setError(caught)
      } else {
        throw caught
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <main className="app">
      <header className="app__header">
        <h1>Garage Fault Assistant</h1>
        <p className="app__subtitle">
          Turn a customer fault description into structured workshop triage.
        </p>
        <Disclaimer />
      </header>

      <FaultForm
        description={description}
        isLoading={isLoading}
        onDescriptionChange={setDescription}
        onSubmit={handleSubmit}
      />

      {result && <ResultPanel result={result} />}
      {error && <ErrorPanel error={error} />}
    </main>
  )
}

export default App
