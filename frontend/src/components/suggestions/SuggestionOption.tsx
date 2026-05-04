import { Group, Text, ActionIcon, Box, Code } from '@mantine/core'
import { IconCopy, IconCheck } from '@tabler/icons-react'
import { useState } from 'react'

interface Props {
  tag: string
  value: string
  index: number
}

function toHtmlSnippet(tag: string, value: string): string {
  const t = tag.toLowerCase()
  if (t === 'meta description') {
    return `<meta name="description" content="${value}" />`
  }
  return `<${t}>${value}</${t}>`
}

export default function SuggestionOption({ tag, value, index }: Props) {
  const [copied, setCopied] = useState(false)
  const snippet = toHtmlSnippet(tag, value)

  function handleCopy() {
    void navigator.clipboard.writeText(snippet).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    })
  }

  return (
    <Box
      style={{
        borderRadius: 8,
        border: '1px solid var(--mantine-color-gray-2)',
        overflow: 'hidden',
      }}
    >
      <Group
        justify="space-between"
        align="center"
        px="sm"
        py={6}
        style={{ background: 'var(--mantine-color-gray-0)', borderBottom: '1px solid var(--mantine-color-gray-2)' }}
      >
        <Text size="xs" fw={600} c="dimmed">
          Option {index + 1}
        </Text>
        <ActionIcon
          variant="subtle"
          size="sm"
          color={copied ? 'green' : 'gray'}
          onClick={handleCopy}
          title="Copy snippet"
        >
          {copied ? <IconCheck size={14} /> : <IconCopy size={14} />}
        </ActionIcon>
      </Group>

      <Box px="sm" py="xs" style={{ background: 'var(--mantine-color-gray-1)' }}>
        <Code
          block
          style={{
            background: 'transparent',
            color: 'var(--mantine-color-gray-8)',
            fontSize: 14,
            padding: 0,
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-word',
            fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
          }}
        >
          {snippet}
        </Code>
      </Box>
    </Box>
  )
}
