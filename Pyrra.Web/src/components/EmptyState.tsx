import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

interface EmptyStateProps {
  /** Ícone opcional no topo do card. */
  icon?: LucideIcon
  /** Cor do ícone; neutra por padrão. Use text-brand-green em casos de conquista. */
  iconClassName?: string
  title: string
  description?: string
  /** CTA opcional (botão ou link), renderizado centralizado abaixo do texto. */
  action?: ReactNode
}

// Estado vazio padrão do app: card centralizado com ícone opcional, título
// convidativo, descrição curta e um slot de CTA. Padroniza os empties que antes
// eram <div>s montados à mão com pequenas variações; o visual segue o do Diário,
// que já era a referência de qualidade.
export function EmptyState({
  icon: Icon,
  iconClassName = 'text-slate-500',
  title,
  description,
  action,
}: EmptyStateProps) {
  return (
    <div className="rounded-md bg-surface px-5 py-10 text-center ring-1 ring-line">
      {Icon && (
        <Icon size={28} className={`mx-auto ${iconClassName}`} aria-hidden="true" />
      )}
      <p
        className={['font-medium text-slate-200', Icon ? 'mt-3' : '']
          .filter(Boolean)
          .join(' ')}
      >
        {title}
      </p>
      {description && (
        <p className="mt-1.5 text-sm text-slate-400">{description}</p>
      )}
      {action && <div className="mt-4 flex justify-center">{action}</div>}
    </div>
  )
}

export default EmptyState
