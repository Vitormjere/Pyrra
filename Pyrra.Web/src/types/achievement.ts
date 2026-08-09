// Espelha os DTOs de Pyrra.Api/Dtos/Achievements.

export type AchievementType = 'Streak' | 'DesafioCompleto' | 'TorneioPodio'

export type AchievementRarity = 'Bronze' | 'Prata' | 'Ouro' | 'Esmeralda' | 'Ametista'

// GET /api/conquistas — catálogo inteiro do usuário, desbloqueada ou não
export interface AchievementResponse {
  id: string
  type: AchievementType
  milestone: number
  /** Só se aplica a Streak; nulo nos demais tipos. */
  rarity: AchievementRarity | null
  xp: number
  name: string
  description: string
  iconKey: string
  unlocked: boolean
  /** ISO 8601, presente só quando unlocked. */
  unlockedAt: string | null
  /** Progresso atual rumo ao marco — presente só quando bloqueada e dá pra calcular (Streak, DesafioCompleto). */
  currentProgress: number | null
}

// GET /api/conquistas/pendentes — desbloqueios ainda não exibidos ao usuário. Carregam Id porque a confirmação pode ser seletiva.
export interface PendingAchievementResponse {
  id: string
  type: AchievementType
  milestone: number
  rarity: AchievementRarity | null
  xp: number
  name: string
  description: string
  iconKey: string
  /** ISO 8601. */
  unlockedAt: string
}
