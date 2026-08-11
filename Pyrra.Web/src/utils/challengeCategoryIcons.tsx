import { Apple, BookOpen, Dumbbell, Flame, Footprints, GraduationCap, Home, Shuffle, Sparkles, Trees, Users } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

// Mapa nome (salvo em ChallengeCategory.Icon, escolhido pelo admin) -> componente lucide-react.
// Sem painel de admin visual ainda: uma categoria nova cadastrada com um ícone que não está aqui
// cai no fallback (Sparkles) até este mapa ser atualizado a mão.
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

export function getCategoryIcon(icon: string): LucideIcon {
  return ICONS[icon] ?? Sparkles
}
