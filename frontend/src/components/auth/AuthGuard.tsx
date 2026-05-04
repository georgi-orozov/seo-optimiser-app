import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@clerk/react'
import { Center, Loader } from '@mantine/core'

export default function AuthGuard() {
  const { isLoaded, isSignedIn } = useAuth()

  if (!isLoaded) return <Center h="100vh"><Loader /></Center>
  if (!isSignedIn) return <Navigate to="/sign-in" replace />

  return <Outlet />
}
