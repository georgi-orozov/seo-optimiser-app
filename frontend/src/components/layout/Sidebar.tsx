import { useEffect, useState } from 'react'
import { Button, ScrollArea, Stack, Skeleton, Text, Modal, TextInput } from '@mantine/core'
import { IconPlus } from '@tabler/icons-react'
import { useChatStore } from '@/store/chatStore'
import { useSessions } from '@/hooks/useSessions'
import SidebarItem from './SidebarItem'

export default function Sidebar() {
  const sessions = useChatStore((s) => s.sessions)
  const sessionsLoaded = useChatStore((s) => s.sessionsLoaded)
  const activeSessionId = useChatStore((s) => s.activeSessionId)
  const { loadSessions, createNewSession, isLoading } = useSessions()

  const [modalOpen, setModalOpen] = useState(false)
  const [sessionName, setSessionName] = useState('')

  useEffect(() => {
    void loadSessions()
  }, [loadSessions])

  function openModal() {
    setSessionName('')
    setModalOpen(true)
  }

  async function handleCreate() {
    const name = sessionName.trim() || 'New Session'
    setModalOpen(false)
    await createNewSession(name)
  }

  return (
    <Stack h="100%" gap={0} p="sm">
      <Button
        leftSection={<IconPlus size={16} />}
        variant="light"
        fullWidth
        mb="sm"
        loading={isLoading}
        onClick={openModal}
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

      <Modal
        opened={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Name your session"
        size="sm"
        centered
      >
        <TextInput
          placeholder="e.g. Homepage SEO audit"
          value={sessionName}
          onChange={(e) => setSessionName(e.currentTarget.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') void handleCreate() }}
          data-autofocus
          mb="md"
        />
        <Button fullWidth onClick={() => void handleCreate()} loading={isLoading}>
          Create Session
        </Button>
      </Modal>
    </Stack>
  )
}
