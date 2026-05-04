import { AppShell, Burger, Group, Text, Title } from '@mantine/core'
import { UserButton } from '@clerk/react'
import { useChatStore } from '@/store/chatStore'
import Sidebar from './Sidebar'
import ChatArea from '@/components/chat/ChatArea'

export default function AppLayout() {
  const sidebarOpen = useChatStore((s) => s.sidebarOpen)
  const setSidebarOpen = useChatStore((s) => s.setSidebarOpen)

  return (
    <AppShell
      navbar={{ width: 280, breakpoint: 'sm', collapsed: { mobile: !sidebarOpen } }}
      header={{ height: 60 }}
      padding={0}
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <Burger
              opened={sidebarOpen}
              onClick={() => setSidebarOpen(!sidebarOpen)}
              hiddenFrom="sm"
              size="sm"
            />
            <Title order={4}>SEO Optimiser</Title>
          </Group>
          <Text size="sm" c="dimmed" visibleFrom="sm">
            AI-powered SEO suggestions
          </Text>
          <UserButton />
        </Group>
      </AppShell.Header>

      <AppShell.Navbar>
        <Sidebar />
      </AppShell.Navbar>

      <AppShell.Main style={{ display: 'flex', flexDirection: 'column', height: '100vh' }}>
        <ChatArea />
      </AppShell.Main>
    </AppShell>
  )
}
