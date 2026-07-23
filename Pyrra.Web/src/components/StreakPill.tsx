import { Flame, Snowflake } from 'lucide-react'

interface StreakPillProps {
  days: number
  freezes: number
}

// Streak em formato compacto. Antes era um card grande no topo do dashboard;
// virou pill porque o número de dias é referência, não a informação principal
// da tela — quem abre o app precisa ver o que FAZER hoje, não o placar.
//
// A escala cresceu junto: ícone 13→18, texto xs→sm, padding 2.5/1→3/1.5. O
// conjunto continua abaixo do H1 ao lado (text-3xl), então ganha presença sem
// disputar com o título da página.
export function StreakPill({ days, freezes }: StreakPillProps) {
  return (
    <div className="flex items-center gap-2">
      <span className="inline-flex items-center gap-1.5 rounded-full bg-surface px-3 py-1.5 ring-1 ring-line">
        <Flame size={18} className="text-brand-green" aria-hidden="true" />
        <span className="text-sm font-semibold text-ink tabular-nums">
          {days}
        </span>
        <span className="sr-only">
          {days === 1 ? 'dia seguido' : 'dias seguidos'}
        </span>
      </span>

      <span
        className="inline-flex items-center gap-1.5 rounded-full bg-surface px-3 py-1.5 ring-1 ring-line"
        title={`${freezes} freeze(s) disponível(is)`}
      >
        <Snowflake size={16} className="text-slate-400" aria-hidden="true" />
        <span className="text-sm font-semibold text-ink tabular-nums">
          {freezes}
        </span>
        <span className="sr-only">freezes disponíveis</span>
      </span>
    </div>
  )
}

export default StreakPill
