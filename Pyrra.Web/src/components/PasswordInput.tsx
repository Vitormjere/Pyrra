import { useState } from 'react'
import type { InputHTMLAttributes } from 'react'
import { Eye, EyeOff } from 'lucide-react'

interface PasswordInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  className?: string
}

// mesmo visual dos inputs de texto do app, mas com pr-11 fixo em vez de px-4:
// o espaço extra à direita é reservado pro botão de olho, então className não
// deve reaplicar px-* (ganharia da ordem do CSS gerado e cobriria o ícone)
const defaultClasses =
  'w-full rounded-md bg-surface py-3 pl-4 pr-11 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

export function PasswordInput({ className, ...props }: PasswordInputProps) {
  const [visible, setVisible] = useState(false)

  return (
    <div className="relative">
      <input {...props} type={visible ? 'text' : 'password'} className={className ?? defaultClasses} />
      <button
        type="button"
        onClick={() => setVisible((current) => !current)}
        aria-label={visible ? 'Ocultar senha' : 'Mostrar senha'}
        className="absolute top-1/2 right-1 -translate-y-1/2 rounded-md p-2 text-slate-500 transition hover:text-ink focus-visible:ring-2 focus-visible:ring-brand-green focus-visible:outline-none"
      >
        {visible ? <EyeOff size={18} aria-hidden="true" /> : <Eye size={18} aria-hidden="true" />}
      </button>
    </div>
  )
}

export default PasswordInput
