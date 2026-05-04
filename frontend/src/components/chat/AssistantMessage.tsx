import { Box, Group, Avatar, Text, Stack } from '@mantine/core'
import { IconRobot } from '@tabler/icons-react'
import type { ChatMessageDto, SuggestionDto } from '@/types/api'
import SuggestionCard from '@/components/suggestions/SuggestionCard'

interface Props {
  message: ChatMessageDto
}

function formatTime(iso: string) {
  return new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' }).format(
    new Date(iso),
  )
}

function groupByTag(suggestions: SuggestionDto[]): Map<string, SuggestionDto[]> {
  return suggestions.reduce((acc, s) => {
    const group = acc.get(s.tag) ?? []
    group.push(s)
    acc.set(s.tag, group)
    return acc
  }, new Map<string, SuggestionDto[]>())
}

export default function AssistantMessage({ message }: Props) {
  const tagGroups =
    message.suggestions && message.suggestions.length > 0
      ? groupByTag(message.suggestions)
      : null

  return (
    <Box className="message-enter" px="md" py="xs">
      <Group align="flex-start" gap="xs" wrap="nowrap">
        <Avatar color="violet" radius="xl" size="sm" style={{ flexShrink: 0, marginTop: 4 }}>
          <IconRobot size={14} />
        </Avatar>

        <Box style={{ maxWidth: '80%' }}>
          <Box
            px="md"
            py="sm"
            style={{
              background: 'var(--mantine-color-gray-1)',
              borderRadius: '4px 18px 18px 18px',
            }}
          >
            <Text size="sm" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {message.content}
            </Text>
          </Box>

          {tagGroups && (
            <Stack gap="xs" mt="sm">
              {Array.from(tagGroups.entries()).map(([tag, suggestions]) => (
                <SuggestionCard key={tag} tag={tag} suggestions={suggestions} />
              ))}
            </Stack>
          )}

          <Text size="xs" c="dimmed" mt={4}>
            {formatTime(message.createdAt)}
          </Text>
        </Box>
      </Group>
    </Box>
  )
}
