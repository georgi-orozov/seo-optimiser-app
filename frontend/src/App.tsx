import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from '@clerk/react'
import { setTokenGetter } from '@/api/client'
import AuthGuard from '@/components/auth/AuthGuard'
import SignInPage from '@/pages/SignInPage'
import ChatPage from '@/pages/ChatPage'

export default function App() {
  const { getToken } = useAuth()
  // Call synchronously so the token getter is registered before any child
  // component effects fire their API requests (child effects run before parent effects).
  setTokenGetter(getToken)

  return (
    <Routes>
      <Route path="/sign-in/*" element={<SignInPage />} />
      <Route element={<AuthGuard />}>
        <Route path="/" element={<ChatPage />} />
        <Route path="/sessions/:sessionId" element={<ChatPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
