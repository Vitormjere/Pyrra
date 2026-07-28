import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { getPendingTeamInvitesCount, joinTeamViaLink } from '../services/teamService'
import { useAuth } from '../hooks/useAuth'
import { PENDING_TEAM_INVITE_KEY, TeamInvitesContext } from './team-invites-context'

// Provider da contagem de convites de time pendentes. Mesmo papel do FriendRequestsProvider, só
// que para convites de time: fica ACIMA do AppLayout, para o badge do menu e a tela de Times
// lerem a mesma contagem, e consome aqui o convite guardado antes do login.
export function TeamInvitesProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [count, setCount] = useState(0)

  const refresh = useCallback(async () => {
    try {
      setCount(await getPendingTeamInvitesCount())
    } catch {
      // Silencioso: o badge é secundário, um erro aqui não deve estourar na tela.
    }
  }, [])

  useEffect(() => {
    if (!user) return
    let active = true

    async function run() {
      try {
        const result = await getPendingTeamInvitesCount()
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

    const token = localStorage.getItem(PENDING_TEAM_INVITE_KEY)
    if (!token) return

    // Remove ANTES de enviar: se falhar, não fica retentando a cada carga.
    localStorage.removeItem(PENDING_TEAM_INVITE_KEY)

    void (async () => {
      try {
        await joinTeamViaLink(token)
      } catch {
        // Convite inválido/expirado ou erro de rede: ignora, o usuário pode tentar de novo.
      }
      navigate('/times', { replace: true })
    })()
  }, [user, navigate])

  const value = useMemo(() => ({ count, refresh }), [count, refresh])

  return (
    <TeamInvitesContext.Provider value={value}>{children}</TeamInvitesContext.Provider>
  )
}

export default TeamInvitesProvider
