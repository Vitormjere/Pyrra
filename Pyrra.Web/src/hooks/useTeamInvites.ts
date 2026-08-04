import { useContext } from 'react'
import { TeamInvitesContext } from '../contexts/team-invites-context'
import type { TeamInvitesContextValue } from '../contexts/team-invites-context'

// contagem de convites de time pendentes + refresh, em arquivo próprio pra não quebrar o fast refresh do provider
export function useTeamInvites(): TeamInvitesContextValue {
  const context = useContext(TeamInvitesContext)
  if (context === undefined) {
    throw new Error('useTeamInvites precisa ser usado dentro de um <TeamInvitesProvider>.')
  }
  return context
}

export default useTeamInvites
