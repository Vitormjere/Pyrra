import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { acceptInvite, getPendingCount } from '../services/friendService'
import { useAuth } from '../hooks/useAuth'
import { FriendRequestsContext, PENDING_INVITE_KEY } from './friend-requests-context'

// fica acima do AppLayout pro badge do menu e a tela de Amigos lerem a mesma contagem, e também
// consome aqui o convite guardado antes do login, já que só monta depois dos gates de onboarding
export function FriendRequestsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [count, setCount] = useState(0)

  // exposto no contexto pra tela de Amigos recarregar após aceitar/recusar um pedido
  const refresh = useCallback(async () => {
    try {
      setCount(await getPendingCount())
    } catch {
      // badge é secundário, erro aqui não deve estourar na tela
    }
  }, [])

  // função async definida dentro do efeito (em vez de chamar refresh) pro eslint confirmar que o setState só roda depois do await
  useEffect(() => {
    if (!user) return
    let active = true

    async function run() {
      try {
        const result = await getPendingCount()
        if (active) setCount(result)
      } catch {
        // mesmo critério do refresh manual, falha aqui não aparece pro usuário
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [user])

  useEffect(() => {
    if (!user) return

    const token = localStorage.getItem(PENDING_INVITE_KEY)
    if (!token) return

    // remove antes de enviar pra não ficar reenviando a cada carga se falhar
    localStorage.removeItem(PENDING_INVITE_KEY)

    void (async () => {
      try {
        await acceptInvite(token)
      } catch {
        // convite inválido/expirado ou erro de rede, ignora e deixa o usuário tentar de novo
      }
      navigate('/amigos', { replace: true })
    })()
  }, [user, navigate])

  const value = useMemo(() => ({ count, refresh }), [count, refresh])

  return (
    <FriendRequestsContext.Provider value={value}>
      {children}
    </FriendRequestsContext.Provider>
  )
}

export default FriendRequestsProvider
