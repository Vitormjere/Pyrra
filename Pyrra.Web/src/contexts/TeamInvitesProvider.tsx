import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { getPendingTeamInvitesCount, joinTeamViaLink } from '../services/teamService'
import { useAuth } from '../hooks/useAuth'
import { PENDING_TEAM_INVITE_KEY, TeamInvitesContext } from './team-invites-context'

// mesmo papel do FriendRequestsProvider, só que pra convites de time
export function TeamInvitesProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [count, setCount] = useState(0)

  const refresh = useCallback(async () => {
    try {
      setCount(await getPendingTeamInvitesCount())
    } catch {
      // badge é secundário, erro aqui não deve estourar na tela
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

    const token = localStorage.getItem(PENDING_TEAM_INVITE_KEY)
    if (!token) return

    // remove antes de enviar pra não ficar retentando a cada carga se falhar
    localStorage.removeItem(PENDING_TEAM_INVITE_KEY)

    void (async () => {
      try {
        await joinTeamViaLink(token)
      } catch {
        // convite inválido/expirado ou erro de rede, ignora e deixa o usuário tentar de novo
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
