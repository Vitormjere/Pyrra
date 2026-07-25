// Bloco de skeleton reutilizável: aplica o pulse e a superfície do tema. Cada
// tela compõe seu próprio layout de carregamento com vários destes, variando só
// a altura via className — antes cada tela repetia a mesma string
// 'animate-pulse rounded-md bg-surface'. O aria-hidden mantém o skeleton fora do
// leitor de tela; quem anuncia "Carregando" é o container aria-busy da tela.
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
