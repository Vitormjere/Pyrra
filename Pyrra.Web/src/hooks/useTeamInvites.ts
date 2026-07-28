import { useContext } from 'react'
import { TeamInvitesContext } from '../contexts/team-invites-context'
import type { TeamInvitesContextValue } from '../contexts/team-invites-context'

// Contagem de convites de time pendentes + refresh. Em arquivo próprio, separado do provider,
// para o Fast Refresh continuar funcionando no arquivo do provider.
export function useTeamInvites(): TeamInvitesContextValue {
  const context = useContext(TeamInvitesContext)
  if (context === undefined) {
    throw new Error('useTeamInvites precisa ser usado dentro de um <TeamInvitesProvider>.')
  }
  return context
}

export default useTeamInvites
