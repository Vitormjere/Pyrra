import type { ReactNode } from 'react'

interface SectionHeaderProps {
  children: ReactNode
  /** Conteúdo alinhado à direita (contador, ação secundária, link). */
  trailing?: ReactNode
}

// Cabeçalho de seção. Ganhou peso em relação à primeira versão: corpo maior,
// bold e uma barra verde à esquerda. Num layout que agora agrupa listas inteiras
// numa superfície só, é o header que marca onde uma seção termina e a próxima
// começa — antes esse trabalho era feito pelos cards separados.
export function SectionHeader({ children, trailing }: SectionHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3">
      {/* Sem uppercase: com o peso forte e a barra verde, a caixa alta somava
          agressividade sem ganho de leitura — e caixa alta é mais lenta de ler
          em textos de duas ou três palavras. O tracking também cedeu, porque
          existia para dar ar às maiúsculas. */}
      <h2 className="glow-ink flex items-center gap-2.5 text-base font-bold tracking-tight text-ink">
        {/* A barra é o único uso decorativo do verde; fina o bastante para
            pontuar sem virar preenchimento. */}
        <span aria-hidden="true" className="h-4 w-0.5 shrink-0 bg-brand-green" />
        {children}
      </h2>
      {trailing}
    </div>
  )
}

export default SectionHeader
