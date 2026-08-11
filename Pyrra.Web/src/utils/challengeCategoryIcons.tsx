import { Apple, BookOpen, Dumbbell, Flame, Footprints, GraduationCap, Home, Shuffle, Sparkles, Trees, Users } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

// Mapa nome (salvo em ChallengeCategory.Icon, escolhido pelo admin) -> componente lucide-react.
// O painel Admin > Desafios só deixa escolher entre essas chaves (CATEGORY_ICON_KEYS); uma
// categoria com ícone fora daqui (cadastro antigo, ou editado direto no banco) cai no fallback
// (Sparkles) até este mapa ganhar uma entrada nova.
const ICONS: Record<string, LucideIcon> = {
  footprints: Footprints,
  dumbbell: Dumbbell,
  apple: Apple,
  flame: Flame,
  shuffle: Shuffle,
  users: Users,
  'graduation-cap': GraduationCap,
  home: Home,
  'book-open': BookOpen,
  trees: Trees,
}

export const CATEGORY_ICON_KEYS = Object.keys(ICONS)

export function getCategoryIcon(icon: string): LucideIcon {
  return ICONS[icon] ?? Sparkles
}
