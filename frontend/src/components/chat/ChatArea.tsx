import { useEffect, useRef } from 'react'
import { Box, Center, Text, Stack } from '@mantine/core'
import { IconMessageCircle } from '@tabler/icons-react'
import { useChatStore } from '@/store/chatStore'
import MessageList from './MessageList'
import TypingIndicator from './TypingIndicator'
import ChatInput from './ChatInput'

export default function ChatArea() {
  const activeSessionId = useChatStore((s) => s.activeSessionId)
  const activeMessages = useChatStore((s) => s.activeMessages)
  const isSending = useChatStore((s) => s.isSending)
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [activeMessages, isSending])

  return (
    <Box style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box ref={scrollRef} style={{ flex: 1, overflowY: 'auto', paddingTop: 12 }}>
        {!activeSessionId ? (
          <Center h="100%">
            <Stack align="center" gap="sm">
              <IconMessageCircle size={48} color="var(--mantine-color-gray-4)" />
              <Text c="dimmed" size="sm">
                Create a new session or select one from the sidebar
              </Text>
            </Stack>
          </Center>
        ) : (
          <>
            <MessageList messages={activeMessages} />
            {isSending && <TypingIndicator />}
          </>
        )}
      </Box>
      <ChatInput />
    </Box>
  )
}
