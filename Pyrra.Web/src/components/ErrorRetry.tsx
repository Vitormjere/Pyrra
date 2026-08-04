interface ErrorRetryProps {
  message: string
  onRetry: () => void
}

// consolida o bloco de erro que vivia idêntico em Hoje, Treino, Tarefas, Finanças, Nutrição e Diário
export function ErrorRetry({ message, onRetry }: ErrorRetryProps) {
  return (
    <div className="flex flex-col items-center gap-4 py-12 text-center">
      <p className="text-sm text-red-300">{message}</p>
      <button
        type="button"
        onClick={onRetry}
        className="rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95"
      >
        Tentar de novo
      </button>
    </div>
  )
}

export default ErrorRetry
