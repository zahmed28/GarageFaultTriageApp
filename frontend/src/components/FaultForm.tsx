import type { FormEvent } from 'react'

interface FaultFormProps {
  description: string
  isLoading: boolean
  onDescriptionChange: (value: string) => void
  onSubmit: () => void
}

export function FaultForm({
  description,
  isLoading,
  onDescriptionChange,
  onSubmit,
}: FaultFormProps) {
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit()
  }

  return (
    <form className="fault-form" onSubmit={handleSubmit}>
      <label className="fault-form__label" htmlFor="description">
        Customer fault description
      </label>
      <textarea
        id="description"
        name="description"
        className="fault-form__textarea"
        rows={6}
        value={description}
        disabled={isLoading}
        placeholder="Paste what the customer said about the fault..."
        onChange={(event) => onDescriptionChange(event.target.value)}
      />
      <div className="fault-form__actions">
        <button type="submit" className="fault-form__submit" disabled={isLoading}>
          {isLoading ? 'Analysing...' : 'Analyse fault'}
        </button>
        {isLoading && (
          <span className="fault-form__loading" role="status" aria-live="polite">
            Analysing fault description
          </span>
        )}
      </div>
    </form>
  )
}
