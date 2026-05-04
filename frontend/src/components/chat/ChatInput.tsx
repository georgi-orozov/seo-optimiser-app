import { useState, type KeyboardEvent } from 'react'
import { Group, Textarea, ActionIcon } from '@mantine/core'
import { IconSend } from '@tabler/icons-react'
import { useChatStore } from '@/store/chatStore'
import { useChat } from '@/hooks/useChat'

export default function ChatInput() {
  const [value, setValue] = useState('')
  const isSending = useChatStore((s) => s.isSending)
  const activeSessionId = useChatStore((s) => s.activeSessionId)
  const { sendMessage } = useChat()

  const disabled = isSending || !activeSessionId

  function handleSend() {
    const trimmed = value.trim()
    if (!trimmed || disabled) return
    setValue('')
    void sendMessage(trimmed)
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <Group gap="xs" px="md" py="sm" style={{ borderTop: '1px solid var(--mantine-color-gray-2)' }}>
      <Textarea
        flex={1}
        placeholder={activeSessionId ? 'Type a message… (Enter to send, Shift+Enter for newline)' : 'Create or select a session to start chatting'}
        value={value}
        onChange={(e) => setValue(e.currentTarget.value)}
        onKeyDown={handleKeyDown}
        autosize
        minRows={1}
        maxRows={6}
        disabled={disabled}
        radius="xl"
        styles={{ input: { paddingRight: 48 } }}
      />
      <ActionIcon
        size="lg"
        radius="xl"
        variant="filled"
        disabled={disabled || !value.trim()}
        onClick={handleSend}
        aria-label="Send message"
      >
        <IconSend size={18} />
      </ActionIcon>
    </Group>
  )
}
