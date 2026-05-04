import axios from 'axios'

type TokenGetter = () => Promise<string | null>
let _getToken: TokenGetter = async () => null

export function setTokenGetter(fn: TokenGetter): void {
  _getToken = fn
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})

apiClient.interceptors.request.use(async (config) => {
  const token = await _getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})
