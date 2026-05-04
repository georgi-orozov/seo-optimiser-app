import { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout'
import { useChat } from '@/hooks/useChat'

export default function ChatPage() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const { loadSession } = useChat()

  useEffect(() => {
    if (sessionId) {
      void loadSession(sessionId)
    }
  }, [sessionId, loadSession])

  return <AppLayout />
}
