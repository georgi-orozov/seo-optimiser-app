import { Group, Text, ActionIcon, Box } from '@mantine/core'
import { IconCopy, IconCheck } from '@tabler/icons-react'
import { useState } from 'react'

interface Props {
  value: string
  index: number
}

export default function SuggestionOption({ value, index }: Props) {
  const [copied, setCopied] = useState(false)

  function handleCopy() {
    void navigator.clipboard.writeText(value).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    })
  }

  return (
    <Group
      gap="sm"
      px="sm"
      py="xs"
      style={{
        borderRadius: 6,
        cursor: 'default',
        transition: 'background 0.15s',
      }}
      styles={{
        root: {
          '&:hover': { background: 'var(--mantine-color-gray-0)' },
        },
      }}
    >
      <Box
        w={22}
        h={22}
        style={{
          borderRadius: '50%',
          background: 'var(--mantine-color-blue-1)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexShrink: 0,
        }}
      >
        <Text size="xs" fw={600} c="blue">
          {index + 1}
        </Text>
      </Box>
      <Text size="sm" flex={1} style={{ wordBreak: 'break-word' }}>
        {value}
      </Text>
      <ActionIcon
        variant="subtle"
        size="sm"
        color={copied ? 'green' : 'gray'}
        onClick={handleCopy}
        title="Copy to clipboard"
      >
        {copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
      </ActionIcon>
    </Group>
  )
}
