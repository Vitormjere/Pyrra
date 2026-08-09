// Mapeamento visual compartilhado entre AchievementUnlockNotice (modal de
// desbloqueio), AchievementCard (grid do perfil) e o modal de compartilhamento —
// centralizado aqui para as três telas não divergirem de cor/ícone/rótulo.
import { Flame, Medal, Trophy } from 'lucide-react'
import type { AchievementRarity, AchievementType } from '../types/achievement'

export const RARITY_LABELS: Record<AchievementRarity, string> = {
  Bronze: 'Bronze',
  Prata: 'Prata',
  Ouro: 'Ouro',
  Esmeralda: 'Esmeralda',
  Ametista: 'Ametista',
}

export const ACHIEVEMENT_TYPE_LABELS: Record<AchievementType, string> = {
  Streak: 'Sequência',
  DesafioCompleto: 'Desafios',
  TorneioPodio: 'Torneios',
}

interface RarityClasses {
  ring: string
  ringSoft: string
  text: string
}

const RARITY_CLASSES: Record<AchievementRarity, RarityClasses> = {
  Bronze: { ring: 'ring-rarity-bronze/40', ringSoft: 'ring-rarity-bronze/30', text: 'text-rarity-bronze' },
  Prata: { ring: 'ring-rarity-prata/40', ringSoft: 'ring-rarity-prata/30', text: 'text-rarity-prata' },
  Ouro: { ring: 'ring-rarity-ouro/40', ringSoft: 'ring-rarity-ouro/30', text: 'text-rarity-ouro' },
  Esmeralda: { ring: 'ring-rarity-esmeralda/40', ringSoft: 'ring-rarity-esmeralda/30', text: 'text-rarity-esmeralda' },
  Ametista: { ring: 'ring-rarity-ametista/40', ringSoft: 'ring-rarity-ametista/30', text: 'text-rarity-ametista' },
}

// conquistas sem raridade (hoje só DesafioCompleto) caem no verde da marca, mesma cor de "ação e conquista" do MilestoneCelebration
const FALLBACK_CLASSES: RarityClasses = { ring: 'ring-brand-green/40', ringSoft: 'ring-brand-green/30', text: 'text-brand-green' }

export function classesForRarity(rarity: AchievementRarity | null): RarityClasses {
  return rarity ? RARITY_CLASSES[rarity] : FALLBACK_CLASSES
}

export function iconForAchievementType(type: AchievementType) {
  if (type === 'Streak') return Flame
  if (type === 'TorneioPodio') return Medal
  return Trophy
}

// unidade do progresso ("23 de 60 dias" / "4 de 10 desafios") — TorneioPodio não tem progresso calculável, não deveria chegar aqui
export function progressUnitLabel(type: AchievementType): string {
  return type === 'Streak' ? 'dias' : 'desafios'
}
