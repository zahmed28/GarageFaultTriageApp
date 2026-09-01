import type { ProblemDetails } from '../types/faultAssessment'

export class ApiError extends Error {
  readonly status: number
  readonly title?: string
  readonly detail?: string
  readonly errors?: Record<string, string[]>
  readonly traceId?: string
  readonly type?: string

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.title = problem.title
    this.detail = problem.detail
    this.errors = problem.errors
    this.traceId = problem.traceId
    this.type = problem.type
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    let problem: ProblemDetails = {
      status: response.status,
      title: response.statusText,
    }

    try {
      const body = (await response.json()) as ProblemDetails
      problem = {
        ...problem,
        ...body,
      }
    } catch {
      // Non-JSON error body — keep status/title defaults.
    }

    return new ApiError(response.status, problem)
  }
}
