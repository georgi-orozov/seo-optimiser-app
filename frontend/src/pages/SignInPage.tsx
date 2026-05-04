import { SignIn } from '@clerk/react'
import { Center } from '@mantine/core'

export default function SignInPage() {
  return (
    <Center h="100vh">
      <SignIn routing="path" path="/sign-in" />
    </Center>
  )
}
