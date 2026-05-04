import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { notifications } from '@mantine/notifications'
import { getSessions, createSession } from '@/api/sessions'
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
    } catch {
      notifications.show({
        color: 'red',
        title: 'Failed to load sessions',
        message: 'Please refresh the page.',
      })
    }
  }, [setSessions])

  const createNewSession = useCallback(async () => {
    setIsLoading(true)
    try {
      const session = await createSession('New Session')
      addSession({
        id: session.id,
        title: session.title,
        createdAt: session.createdAt,
        updatedAt: session.createdAt,
      })
      navigate(`/sessions/${session.id}`)
    } catch {
      notifications.show({
        color: 'red',
        title: 'Failed to create session',
        message: 'Please try again.',
      })
    } finally {
      setIsLoading(false)
    }
  }, [addSession, navigate])

  return { loadSessions, createNewSession, isLoading }
}
