import { Card, Badge, Text, Stack, Divider } from '@mantine/core'
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
          <Text size="xs" c="dimmed" mb="xs">
            Current: <em>{currentValue}</em>
          </Text>
          <Divider mb="xs" />
        </>
      )}

      <Stack gap={4}>
        {suggestions.map((s, i) => (
          <SuggestionOption key={i} value={s.suggestedValue} index={i} />
        ))}
      </Stack>
    </Card>
  )
}
