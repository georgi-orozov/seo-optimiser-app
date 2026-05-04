import { Box, Paper, Text } from '@mantine/core'
import type { ChatMessageDto } from '@/types/api'

interface Props {
  message: ChatMessageDto
}

function formatTime(iso: string) {
  return new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' }).format(
    new Date(iso),
  )
}

export default function MessageBubble({ message }: Props) {
  return (
    <Box className="message-enter" style={{ display: 'flex', justifyContent: 'flex-end' }} px="md" py="xs">
      <Box style={{ maxWidth: '70%' }}>
        <Paper
          px="md"
          py="sm"
          style={{
            background: 'var(--mantine-color-blue-6)',
            color: 'white',
            borderRadius: '18px 18px 4px 18px',
          }}
        >
          <Text size="sm" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
            {message.content}
          </Text>
        </Paper>
        <Text size="xs" c="dimmed" ta="right" mt={4}>
          {formatTime(message.createdAt)}
        </Text>
      </Box>
    </Box>
  )
}
