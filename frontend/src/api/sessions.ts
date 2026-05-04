import { apiClient } from './client'
import type { CreateSessionResult, SessionDetailDto, SessionSummaryDto } from '@/types/api'

export async function getSessions(): Promise<SessionSummaryDto[]> {
  const { data } = await apiClient.get<SessionSummaryDto[]>('/api/sessions')
  return data
}

export async function getSessionById(id: string): Promise<SessionDetailDto> {
  const { data } = await apiClient.get<SessionDetailDto>(`/api/sessions/${id}`)
  return data
}

export async function createSession(title?: string): Promise<CreateSessionResult> {
  const { data } = await apiClient.post<CreateSessionResult>('/api/sessions', { title })
  return data
}
