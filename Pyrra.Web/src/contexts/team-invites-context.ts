import { createContext } from 'react'

// Contagem de convites de time pendentes, compartilhada entre o badge do menu e a tela de Times
// (que chama refresh após aceitar/recusar). Em arquivo próprio, separado do provider, mesmo
// motivo do friend-requests-context: manter o Fast Refresh funcionando no arquivo do provider.
export interface TeamInvitesContextValue {
  count: number
  refresh: () => Promise<void>
}

export const TeamInvitesContext = createContext<TeamInvitesContextValue | undefined>(undefined)

// Chave do convite de time guardado quando alguém abre um link deslogado — consumido depois que
// a sessão existe. Mesmo padrão de PENDING_INVITE_KEY, chave própria para não colidir com o
// convite de amizade.
export const PENDING_TEAM_INVITE_KEY = 'pyrra.pendingTeamInvite'
