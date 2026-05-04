import { Box, Group, Avatar, Text, Paper } from '@mantine/core'
import { IconUser } from '@tabler/icons-react'
import { useUser } from '@clerk/react'
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
  const { user } = useUser()
  const fullName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || undefined

  return (
    <Box className="message-enter" px="md" py="xs">
      <Group justify="flex-end" align="flex-end" gap="xs" wrap="nowrap">
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
        <Avatar
          name={fullName}
          src={user?.imageUrl}
          color="blue"
          radius="xl"
          size="sm"
          style={{ flexShrink: 0, marginBottom: 18 }}
        >
          <IconUser size={14} />
        </Avatar>
      </Group>
    </Box>
  )
}
