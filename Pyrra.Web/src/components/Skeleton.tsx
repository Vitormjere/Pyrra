// bloco reutilizável de pulse, cada tela monta seu layout de carregamento variando só a altura
export function Skeleton({ className }: { className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={['animate-pulse rounded-md bg-surface', className]
        .filter(Boolean)
        .join(' ')}
    />
  )
}

export default Skeleton
