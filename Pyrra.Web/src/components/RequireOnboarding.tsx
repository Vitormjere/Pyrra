import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

// gate do onboarding, aninhado no ProtectedRoute e fora da rota /onboarding pra não entrar em loop de redirect
export function RequireOnboarding() {
  const { user } = useAuth()

  if (user && !user.onboardingCompleted) {
    return <Navigate to="/onboarding" replace />
  }

  return <Outlet />
}

export default RequireOnboarding
