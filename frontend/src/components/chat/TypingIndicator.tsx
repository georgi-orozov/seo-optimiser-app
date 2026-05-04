import { Group, Box, Avatar } from '@mantine/core'
import { IconRobot } from '@tabler/icons-react'

export default function TypingIndicator() {
  return (
    <Group px="md" py="xs" align="flex-start" gap="xs" wrap="nowrap">
      <Avatar color="violet" radius="xl" size="sm" style={{ flexShrink: 0, marginTop: 4 }}>
        <IconRobot size={14} />
      </Avatar>
      <Box
        px="md"
        py="sm"
        style={{
          background: 'var(--mantine-color-gray-1)',
          borderRadius: '4px 18px 18px 18px',
          display: 'flex',
          gap: 4,
          alignItems: 'center',
          minHeight: 40,
        }}
      >
        <span className="dot-bounce" style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--mantine-color-gray-5)', display: 'inline-block' }} />
        <span className="dot-bounce" style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--mantine-color-gray-5)', display: 'inline-block' }} />
        <span className="dot-bounce" style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--mantine-color-gray-5)', display: 'inline-block' }} />
      </Box>
    </Group>
  )
}
