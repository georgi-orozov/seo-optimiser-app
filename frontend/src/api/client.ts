import axios, { type AxiosError } from 'axios'

// ── ProblemDetails shape (RFC 9457) ───────────────────────────────────────────
interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}

function isProblemDetails(data: unknown): data is ProblemDetails {
  return typeof data === 'object' && data !== null && ('title' in data || 'detail' in data)
}

// ── Typed API error ───────────────────────────────────────────────────────────
export class ApiError extends Error {
  readonly statusCode: number

  constructor(message: string, statusCode: number) {
    super(message)
    this.name = 'ApiError'
    this.statusCode = statusCode
  }
}

// ── Token getter ──────────────────────────────────────────────────────────────
type TokenGetter = () => Promise<string | null>
let _getToken: TokenGetter = async () => null

export function setTokenGetter(fn: TokenGetter): void {
  _getToken = fn
}

// ── Axios instance ────────────────────────────────────────────────────────────
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})

// Request interceptor: attach JWT
apiClient.interceptors.request.use(async (config) => {
  const token = await _getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor: normalise errors into ApiError
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const statusCode = error.response?.status ?? 0
    const data = error.response?.data

    let message: string
    if (isProblemDetails(data)) {
      message = data.detail ?? data.title ?? error.message
    } else if (statusCode === 401) {
      message = 'You are not authorised. Please sign in again.'
    } else if (statusCode === 0) {
      message = 'Cannot reach the server. Check your connection.'
    } else {
      message = error.message
    }

    return Promise.reject(new ApiError(message, statusCode))
  },
)
