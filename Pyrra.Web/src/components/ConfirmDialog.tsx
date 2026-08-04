import { useEffect } from 'react'

interface ConfirmDialogProps {
  title: string
  message?: string
  confirmLabel?: string
  cancelLabel?: string
  /** Ação destrutiva (remoções): botão de confirmar em vermelho em vez do verde. */
  destructive?: boolean
  onConfirm: () => void
  onCancel: () => void
}

// modal com a identidade do app no lugar do window.confirm nativo — overlay escuro, esc ou toque fora cancela
export function ConfirmDialog({
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  destructive = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  // esc cancela, senão o modal vira uma parede pra quem navega por teclado
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onCancel()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onCancel])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Fundo clicável: cancelar tocando fora é o gesto esperado num overlay,
          mesmo do menu de adição da Agenda. */}
      <button
        type="button"
        aria-label={cancelLabel}
        onClick={onCancel}
        className="absolute inset-0 bg-brand-dark/80 backdrop-blur-sm"
      />

      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        className="relative w-full max-w-sm rounded-md bg-surface px-6 py-6 ring-1 ring-line"
      >
        <h2
          id="confirm-dialog-title"
          className="font-display text-xl font-semibold tracking-tight text-ink"
        >
          {title}
        </h2>

        {message && (
          <p className="mt-2 text-sm leading-relaxed text-slate-400">{message}</p>
        )}

        <div className="mt-6 flex gap-2">
          <button
            type="button"
            onClick={onCancel}
            className="flex-1 rounded-xl px-4 py-2.5 font-medium text-slate-300 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink"
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            autoFocus
            onClick={onConfirm}
            className={[
              'flex-1 rounded-xl px-4 py-2.5 font-semibold transition hover:brightness-95',
              destructive
                ? 'bg-red-500 text-white'
                : 'bg-brand-green text-brand-dark',
            ].join(' ')}
          >
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}

export default ConfirmDialog
