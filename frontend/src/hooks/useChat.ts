import { useCallback } from 'react'
import { notifications } from '@mantine/notifications'
import { sendMessage as sendMessageApi } from '@/api/messages'
import { getSessionById } from '@/api/sessions'
import { ApiError } from '@/api/client'
import { useChatStore } from '@/store/chatStore'

export function useChat() {
  const activeSessionId = useChatStore((s) => s.activeSessionId)
  const setActiveSessionId = useChatStore((s) => s.setActiveSessionId)
  const setActiveMessages = useChatStore((s) => s.setActiveMessages)
  const appendMessage = useChatStore((s) => s.appendMessage)
  const removeMessage = useChatStore((s) => s.removeMessage)
  const setIsSending = useChatStore((s) => s.setIsSending)

  const loadSession = useCallback(
    async (id: string) => {
      setActiveSessionId(id)
      try {
        const session = await getSessionById(id)
        setActiveMessages(session.messages)
      } catch (err) {
        notifications.show({
          color: 'red',
          title: 'Failed to load session',
          message: err instanceof ApiError ? err.message : 'Please try again.',
        })
      }
    },
    [setActiveSessionId, setActiveMessages],
  )

  const sendMessage = useCallback(
    async (content: string) => {
      if (!activeSessionId) return

      const optimisticId = crypto.randomUUID()
      appendMessage({
        id: optimisticId,
        role: 'User',
        content,
        createdAt: new Date().toISOString(),
      })
      setIsSending(true)

      try {
        const result = await sendMessageApi(activeSessionId, content)
        appendMessage(result.assistantMessage)
      } catch (err) {
        removeMessage(optimisticId)
        notifications.show({
          color: 'red',
          title: 'Failed to send message',
          message: err instanceof ApiError ? err.message : 'Please try again.',
        })
      } finally {
        setIsSending(false)
      }
    },
    [activeSessionId, appendMessage, removeMessage, setIsSending],
  )

  return { loadSession, sendMessage }
}
