import { NavLink } from '@mantine/core'
import { useNavigate } from 'react-router-dom'
import { IconMessage } from '@tabler/icons-react'
import { useChatStore } from '@/store/chatStore'
import type { SessionSummaryDto } from '@/types/api'

interface Props {
  session: SessionSummaryDto
  isActive: boolean
}

export default function SidebarItem({ session, isActive }: Props) {
  const navigate = useNavigate()
  const setSidebarOpen = useChatStore((s) => s.setSidebarOpen)

  function handleClick() {
    navigate(`/sessions/${session.id}`)
    setSidebarOpen(false)
  }

  return (
    <NavLink
      label={session.title}
      leftSection={<IconMessage size={16} />}
      active={isActive}
      onClick={handleClick}
      style={{ borderRadius: 6 }}
    />
  )
}
