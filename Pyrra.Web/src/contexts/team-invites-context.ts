import { createContext } from 'react'

// contagem de convites de time pendentes, compartilhada entre o badge do menu e a tela de Times
export interface TeamInvitesContextValue {
  count: number
  refresh: () => Promise<void>
}

export const TeamInvitesContext = createContext<TeamInvitesContextValue | undefined>(undefined)

// chave do convite de time guardado quando alguém abre um link deslogado, própria pra não colidir com o convite de amizade
export const PENDING_TEAM_INVITE_KEY = 'pyrra.pendingTeamInvite'
