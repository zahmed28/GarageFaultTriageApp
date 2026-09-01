import type {
  AnalyseFaultRequest,
  AnalyseFaultResponse,
} from '../types/faultAssessment'
import { ApiError } from './apiError'

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

export async function analyseFault(
  description: string,
): Promise<AnalyseFaultResponse> {
  const request: AnalyseFaultRequest = { description }

  const response = await fetch(`${baseUrl}/api/fault-assessments/analyse`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (response.ok) {
    return (await response.json()) as AnalyseFaultResponse
  }

  throw await ApiError.fromResponse(response)
}
