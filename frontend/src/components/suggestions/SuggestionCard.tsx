import { Card, Badge, Text, Stack, Divider, Code, Box } from '@mantine/core'
import type { SuggestionDto } from '@/types/api'
import SuggestionOption from './SuggestionOption'

interface Props {
  tag: string
  suggestions: SuggestionDto[]
}

const TAG_COLORS: Record<string, string> = {
  title: 'blue',
  'meta description': 'violet',
  h1: 'teal',
}

function toHtmlSnippet(tag: string, value: string): string {
  const t = tag.toLowerCase()
  if (t === 'meta description') {
    return `<meta name="description" content="${value}" />`
  }
  return `<${t}>${value}</${t}>`
}

export default function SuggestionCard({ tag, suggestions }: Props) {
  const currentValue = suggestions[0]?.currentValue
  const color = TAG_COLORS[tag.toLowerCase()] ?? 'gray'

  return (
    <Card withBorder radius="md" p="sm" style={{ width: '100%' }}>
      <Badge color={color} variant="light" mb="xs" tt="capitalize">
        {tag}
      </Badge>

      {currentValue && (
        <>
          <Text size="xs" c="dimmed" mb={6}>
            Current:
          </Text>
          <Box
            mb="xs"
            style={{
              borderRadius: 6,
              overflow: 'hidden',
              border: '1px solid var(--mantine-color-gray-2)',
              background: 'var(--mantine-color-gray-1)',
            }}
          >
            <Code
              block
              style={{
                background: 'transparent',
                color: 'var(--mantine-color-gray-6)',
                fontSize: 13,
                padding: '6px 10px',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
              }}
            >
              {toHtmlSnippet(tag, currentValue)}
            </Code>
          </Box>
          <Divider mb="xs" />
        </>
      )}

      <Stack gap="xs">
        {suggestions.map((s, i) => (
          <SuggestionOption key={i} tag={tag} value={s.suggestedValue} index={i} />
        ))}
      </Stack>
    </Card>
  )
}
