import { Check } from 'lucide-react'

interface CheckCircleProps {
  checked: boolean
}

// contorno vira verde com check ao concluir, sem virar um bloco de cor chapado na lista
export function CheckCircle({ checked }: CheckCircleProps) {
  return (
    <span
      aria-hidden="true"
      className={[
        'flex size-5 shrink-0 items-center justify-center rounded-full ring-1 transition',
        checked
          ? 'text-brand-green ring-brand-green/60'
          : 'text-transparent ring-slate-600',
      ].join(' ')}
    >
      <Check size={12} strokeWidth={3} />
    </span>
  )
}

export default CheckCircle
