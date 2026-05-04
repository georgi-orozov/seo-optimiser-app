import { useEffect } from 'react'
import { Button, ScrollArea, Stack, Skeleton, Text } from '@mantine/core'
import { IconPlus } from '@tabler/icons-react'
import { useChatStore } from '@/store/chatStore'
import { useSessions } from '@/hooks/useSessions'
import SidebarItem from './SidebarItem'

export default function Sidebar() {
  const sessions = useChatStore((s) => s.sessions)
  const sessionsLoaded = useChatStore((s) => s.sessionsLoaded)
  const activeSessionId = useChatStore((s) => s.activeSessionId)
  const { loadSessions, createNewSession, isLoading } = useSessions()

  useEffect(() => {
    void loadSessions()
  }, [loadSessions])

  return (
    <Stack h="100%" gap={0} p="sm">
      <Button
        leftSection={<IconPlus size={16} />}
        variant="light"
        fullWidth
        mb="sm"
        loading={isLoading}
        onClick={() => void createNewSession()}
      >
        New Session
      </Button>

      <ScrollArea flex={1} offsetScrollbars>
        {!sessionsLoaded ? (
          <Stack gap="xs">
            <Skeleton height={36} radius="sm" />
            <Skeleton height={36} radius="sm" />
            <Skeleton height={36} radius="sm" />
          </Stack>
        ) : sessions.length === 0 ? (
          <Text c="dimmed" size="sm" ta="center" mt="xl">
            No sessions yet
          </Text>
        ) : (
          <Stack gap={4}>
            {sessions.map((session) => (
              <SidebarItem
                key={session.id}
                session={session}
                isActive={session.id === activeSessionId}
              />
            ))}
          </Stack>
        )}
      </ScrollArea>
    </Stack>
  )
}
