export interface SuggestionDto {
  tag: string
  currentValue?: string
  suggestedValue: string
}

export interface ChatMessageDto {
  id: string
  role: 'User' | 'Assistant'
  content: string
  createdAt: string
  suggestions?: SuggestionDto[]
}

export interface SessionSummaryDto {
  id: string
  title: string
  createdAt: string
  updatedAt: string
}

export interface SessionDetailDto {
  id: string
  title: string
  createdAt: string
  messages: ChatMessageDto[]
}

export interface CreateSessionResult {
  id: string
  title: string
  createdAt: string
}

export interface SendMessageResult {
  assistantMessage: ChatMessageDto
}
