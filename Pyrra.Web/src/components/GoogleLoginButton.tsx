import { GoogleLogin } from '@react-oauth/google'
import type { CredentialResponse } from '@react-oauth/google'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import { getApiErrorMessage } from '../services/apiError'

interface GoogleLoginButtonProps {
  // dono da tela decide onde/como mostrar o erro (mesmo padrão de estado de erro que o form já usa)
  onError: (message: string) => void
}

const FALLBACK_ERROR = 'Não foi possível entrar com o Google. Tente novamente.'

// Botão pronto do próprio @react-oauth/google (iframe renderizado pelo Google) — mesma decisão
// de "usar a lib oficial" do CAPTCHA: sem isso, teríamos que desenhar o logo do Google e montar
// o fluxo de popup/credencial na mão. theme="filled_black" é o que mais se aproxima do resto da
// UI (fundo escuro, sem branco puro).
export function GoogleLoginButton({ onError }: GoogleLoginButtonProps) {
  const { loginWithGoogle } = useAuth()
  const navigate = useNavigate()

  async function handleSuccess(credential: CredentialResponse) {
    if (!credential.credential) {
      onError(FALLBACK_ERROR)
      return
    }

    try {
      await loginWithGoogle(credential.credential)
      // replace: mesmo raciocínio do login por senha — não fica no histórico
      navigate('/hoje', { replace: true })
    } catch (err) {
      onError(getApiErrorMessage(err, {}, FALLBACK_ERROR))
    }
  }

  return (
    <GoogleLogin
      onSuccess={handleSuccess}
      onError={() => onError(FALLBACK_ERROR)}
      theme="filled_black"
      shape="pill"
      size="large"
      text="continue_with"
      width="384"
    />
  )
}

export default GoogleLoginButton
