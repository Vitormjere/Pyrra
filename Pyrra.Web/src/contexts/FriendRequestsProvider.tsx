import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { acceptInvite, getPendingCount } from '../services/friendService'
import { useAuth } from '../hooks/useAuth'
import { FriendRequestsContext, PENDING_INVITE_KEY } from './friend-requests-context'

// Provider da contagem de pedidos pendentes. Fica ACIMA do AppLayout (por isso é uma rota de
// layout com Outlet), para o badge do menu e a tela de Amigos lerem a mesma contagem.
//
// Também consome aqui o convite guardado antes do login: como este provider só monta depois dos
// gates de sessão/onboarding/username, o pedido só é enviado quando o usuário já está completo.
export function FriendRequestsProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [count, setCount] = useState(0)

  // Exposto no contexto para a tela de Amigos recarregar após aceitar/recusar um pedido — uma
  // chamada de evento, não dentro de um efeito, então pode fazer setState livremente.
  const refresh = useCallback(async () => {
    try {
      setCount(await getPendingCount())
    } catch {
      // Silencioso: o badge é secundário, um erro aqui não deve estourar na tela.
    }
  }, [])

  // Carga inicial: função assíncrona definida DENTRO do efeito (em vez de chamar `refresh`
  // diretamente) para o setState só acontecer depois do await, como a regra
  // react-hooks/set-state-in-effect exige — chamar uma função externa (mesmo useCallback) não
  // deixa o analisador confirmar isso.
  useEffect(() => {
    if (!user) return
    let active = true

    async function run() {
      try {
        const result = await getPendingCount()
        if (active) setCount(result)
      } catch {
        // Silencioso, mesmo critério do refresh manual.
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

    // Remove ANTES de enviar: se falhar, não fica reenviando a cada carga.
    localStorage.removeItem(PENDING_INVITE_KEY)

    void (async () => {
      try {
        await acceptInvite(token)
      } catch {
        // Convite inválido/expirado ou erro de rede: ignora, o usuário pode tentar de novo.
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
