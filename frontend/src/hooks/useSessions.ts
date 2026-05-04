import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { notifications } from '@mantine/notifications'
import { getSessions, createSession } from '@/api/sessions'
import { ApiError } from '@/api/client'
import { useChatStore } from '@/store/chatStore'

export function useSessions() {
  const [isLoading, setIsLoading] = useState(false)
  const setSessions = useChatStore((s) => s.setSessions)
  const addSession = useChatStore((s) => s.addSession)
  const navigate = useNavigate()

  const loadSessions = useCallback(async () => {
    try {
      const data = await getSessions()
      setSessions(data)
    } catch (err) {
      // 401 means Clerk hasn't authenticated yet — a fresh user always starts with 0 sessions.
      if (err instanceof ApiError && err.statusCode === 401) {
        setSessions([])
        return
      }
      setSessions([])
      notifications.show({
        color: 'red',
        title: 'Could not load sessions',
        message: err instanceof ApiError ? err.message : 'Please refresh the page.',
      })
    }
  }, [setSessions])

  const createNewSession = useCallback(async (title: string) => {
    setIsLoading(true)
    try {
      const session = await createSession(title)
      addSession({
        id: session.id,
        title: session.title,
        createdAt: session.createdAt,
        updatedAt: session.createdAt,
      })
      navigate(`/sessions/${session.id}`)
    } catch (err) {
      notifications.show({
        color: 'red',
        title: 'Failed to create session',
        message: err instanceof ApiError ? err.message : 'Please try again.',
      })
    } finally {
      setIsLoading(false)
    }
  }, [addSession, navigate])

  return { loadSessions, createNewSession, isLoading }
}
