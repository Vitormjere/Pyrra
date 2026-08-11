import { getCategoryIcon } from '../utils/challengeCategoryIcons'
import { CHALLENGE_CATEGORY_SWATCH } from '../utils/challengeCategoryColors'
import type { TeamCategoryStatus } from '../types/challenges'

// linha de categoria com botão Ativar/Desativar — usada na criação de time (seleção inicial) e
// no detalhe do time (seção Categorias ativas), mesmo visual nos dois lugares
export function CategoryToggleRow({
  category,
  busy,
  onToggle,
}: {
  category: TeamCategoryStatus
  busy: boolean
  onToggle: () => void
}) {
  const Icon = getCategoryIcon(category.icon)
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <span
        aria-hidden="true"
        className={[
          'flex size-9 shrink-0 items-center justify-center rounded-full text-brand-dark',
          CHALLENGE_CATEGORY_SWATCH[category.color],
        ].join(' ')}
      >
        <Icon size={16} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{category.name}</p>
        {category.description && (
          <p className="truncate text-xs text-slate-500">{category.description}</p>
        )}
      </div>
      <button
        type="button"
        disabled={busy}
        onClick={onToggle}
        aria-pressed={category.isActive}
        className={[
          'shrink-0 rounded-xl px-3 py-1.5 text-xs font-semibold transition disabled:opacity-50',
          category.isActive
            ? 'text-slate-400 ring-1 ring-line hover:bg-surface-hi hover:text-ink'
            : 'bg-brand-green text-brand-dark hover:brightness-95',
        ].join(' ')}
      >
        {category.isActive ? 'Desativar' : 'Ativar'}
      </button>
    </li>
  )
}

export default CategoryToggleRow
