import type { ChatMessageDto } from '@/types/api'
import MessageBubble from './MessageBubble'
import AssistantMessage from './AssistantMessage'

interface Props {
  messages: ChatMessageDto[]
}

export default function MessageList({ messages }: Props) {
  return (
    <>
      {messages.map((message) =>
        message.role === 'User' ? (
          <MessageBubble key={message.id} message={message} />
        ) : (
          <AssistantMessage key={message.id} message={message} />
        ),
      )}
    </>
  )
}
