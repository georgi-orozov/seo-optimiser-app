import { Group, Box } from '@mantine/core'

export default function TypingIndicator() {
  return (
    <Group gap={4} px="md" py="xs" align="flex-end">
      <Box
        w={32}
        h={32}
        style={{
          borderRadius: '50%',
          background: 'var(--mantine-color-gray-2)',
          flexShrink: 0,
        }}
      />
      <Box
        px="md"
        py="sm"
        style={{
          background: 'var(--mantine-color-gray-1)',
          borderRadius: '18px 18px 18px 4px',
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
