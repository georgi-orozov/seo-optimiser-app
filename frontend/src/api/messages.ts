import { apiClient } from './client'
import type { SendMessageResult } from '@/types/api'

export async function sendMessage(
  sessionId: string,
  content: string,
): Promise<SendMessageResult> {
  const { data } = await apiClient.post<SendMessageResult>(
    `/api/sessions/${sessionId}/messages`,
    { content },
  )
  return data
}
