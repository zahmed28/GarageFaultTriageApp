export interface AnalyseFaultRequest {
  description: string
}

export interface AnalyseFaultResponse {
  customerConcern: string
  vehicleSystem: string
  urgency: string
  symptoms: string[]
  workshopChecks: string[]
  clarifyingQuestions: string[]
  safetyWarning?: string
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errors?: Record<string, string[]>
}
