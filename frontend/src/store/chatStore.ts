import { create } from 'zustand'
import type { ChatMessageDto, SessionSummaryDto } from '@/types/api'

interface ChatState {
  sessions: SessionSummaryDto[]
  sessionsLoaded: boolean

  activeSessionId: string | null
  activeMessages: ChatMessageDto[]
  activeSessionLoaded: boolean

  isSending: boolean
  sidebarOpen: boolean

  setSessions: (sessions: SessionSummaryDto[]) => void
  addSession: (session: SessionSummaryDto) => void
  setActiveSessionId: (id: string | null) => void
  setActiveMessages: (messages: ChatMessageDto[]) => void
  appendMessage: (message: ChatMessageDto) => void
  removeMessage: (id: string) => void
  setIsSending: (v: boolean) => void
  setSidebarOpen: (v: boolean) => void
  resetActiveSession: () => void
}

export const useChatStore = create<ChatState>((set) => ({
  sessions: [],
  sessionsLoaded: false,

  activeSessionId: null,
  activeMessages: [],
  activeSessionLoaded: false,

  isSending: false,
  sidebarOpen: false,

  setSessions: (sessions) => set({ sessions, sessionsLoaded: true }),

  addSession: (session) =>
    set((state) => ({ sessions: [session, ...state.sessions] })),

  setActiveSessionId: (id) => set({ activeSessionId: id }),

  setActiveMessages: (messages) =>
    set({ activeMessages: messages, activeSessionLoaded: true }),

  appendMessage: (message) =>
    set((state) => ({ activeMessages: [...state.activeMessages, message] })),

  removeMessage: (id) =>
    set((state) => ({
      activeMessages: state.activeMessages.filter((m) => m.id !== id),
    })),

  setIsSending: (v) => set({ isSending: v }),

  setSidebarOpen: (v) => set({ sidebarOpen: v }),

  resetActiveSession: () =>
    set({ activeSessionId: null, activeMessages: [], activeSessionLoaded: false }),
}))
