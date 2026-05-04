import { Component, type ErrorInfo, type ReactNode } from 'react'
import { Center, Stack, Title, Text, Button } from '@mantine/core'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
}

export default class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('[ErrorBoundary] Unhandled render error:', error, info.componentStack)
  }

  private handleReset = (): void => {
    this.setState({ hasError: false })
  }

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <Center h="100vh">
          <Stack align="center" gap="md">
            <Title order={3}>Something went wrong</Title>
            <Text c="dimmed" size="sm" maw={400} ta="center">
              An unexpected error occurred. Please reload the page or contact support if the problem
              persists.
            </Text>
            <Button onClick={this.handleReset} variant="light">
              Try again
            </Button>
          </Stack>
        </Center>
      )
    }

    return this.props.children
  }
}
