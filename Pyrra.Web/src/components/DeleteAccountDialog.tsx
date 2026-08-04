import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { getApiErrorMessage } from '../services/apiError'

interface DeleteAccountDialogProps {
  onConfirm: (currentPassword: string) => Promise<void>
  onCancel: () => void
}

// exclusão de conta exige a senha atual (reautenticação), por isso é um componente à parte em vez de variante do ConfirmDialog genérico
export function DeleteAccountDialog({ onConfirm, onCancel }: DeleteAccountDialogProps) {
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !submitting) onCancel()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onCancel, submitting])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!password || submitting) return

    setSubmitting(true)
    setError(null)

    try {
      await onConfirm(password)
      // sem setSubmitting(false) aqui: o chamador desmonta este diálogo, mexer em estado depois seria setState num componente saindo
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível excluir sua conta.'),
      )
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        aria-label="Cancelar"
        onClick={() => !submitting && onCancel()}
        className="absolute inset-0 bg-brand-dark/80 backdrop-blur-sm"
      />

      <form
        onSubmit={handleSubmit}
        role="dialog"
        aria-modal="true"
        aria-labelledby="delete-account-title"
        className="relative w-full max-w-sm rounded-md bg-surface px-6 py-6 ring-1 ring-line"
      >
        <h2
          id="delete-account-title"
          className="font-display text-xl font-semibold tracking-tight text-ink"
        >
          Excluir conta
        </h2>

        <p className="mt-2 text-sm leading-relaxed text-slate-400">
          Isso desativa sua conta e todos os seus dados deixam de ser
          acessíveis. Para confirmar, digite sua senha atual.
        </p>

        <div className="mt-4 flex flex-col gap-1.5">
          <label htmlFor="delete-password" className="text-xs font-medium text-slate-400">
            Senha atual
          </label>
          <input
            id="delete-password"
            type="password"
            value={password}
            onChange={(event) => {
              setPassword(event.target.value)
              if (error) setError(null)
            }}
            autoComplete="current-password"
            autoFocus
            className="w-full rounded-md bg-surface-hi px-4 py-2.5 text-ink ring-1 ring-line transition outline-none focus:ring-2 focus:ring-red-400"
          />
        </div>

        {error && (
          <p role="alert" className="mt-2 text-sm text-red-300">
            {error}
          </p>
        )}

        <div className="mt-6 flex gap-2">
          <button
            type="button"
            onClick={onCancel}
            disabled={submitting}
            className="flex-1 rounded-xl px-4 py-2.5 font-medium text-slate-300 ring-1 ring-line transition hover:bg-surface-hi hover:text-ink disabled:opacity-60"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={submitting || !password}
            className="flex-1 rounded-xl bg-red-500 px-4 py-2.5 font-semibold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {submitting ? 'Excluindo…' : 'Excluir conta'}
          </button>
        </div>
      </form>
    </div>
  )
}

export default DeleteAccountDialog
