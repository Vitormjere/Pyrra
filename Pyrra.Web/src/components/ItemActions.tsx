import { Pencil, Trash2 } from 'lucide-react'

interface ItemActionsProps {
  onEdit: () => void
  onDelete: () => void
  /** Trava os dois botões enquanto uma remoção deste item está em voo. */
  busy?: boolean
  editLabel: string
  deleteLabel: string
}

// Par editar/remover, idêntico em todas as listas. Ícones pequenos e discretos —
// a ação principal de cada linha continua sendo o toque no conteúdo, não nestes
// controles.
export function ItemActions({
  onEdit,
  onDelete,
  busy = false,
  editLabel,
  deleteLabel,
}: ItemActionsProps) {
  return (
    <span className="flex shrink-0 items-center gap-0.5">
      <button
        type="button"
        onClick={onEdit}
        disabled={busy}
        aria-label={editLabel}
        className="rounded p-1.5 text-slate-500 transition hover:bg-surface-hi hover:text-slate-200 disabled:opacity-50"
      >
        <Pencil size={15} />
      </button>
      <button
        type="button"
        onClick={onDelete}
        disabled={busy}
        aria-label={deleteLabel}
        className="rounded p-1.5 text-slate-500 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
      >
        <Trash2 size={15} />
      </button>
    </span>
  )
}

export default ItemActions
