interface ProgressRingProps {
  /** 0 a 100. */
  percent: number
  /** Número grande no centro. */
  value: string
  /** Rótulo pequeno abaixo do número. */
  label: string
  /** Verde pleno no arco — reservado à meta batida. */
  accent?: boolean
  size?: number
}

// Anel de progresso. O arco é um círculo SVG com stroke-dasharray: o traço
// desenhado equivale ao percentual e o resto fica vazado sobre a trilha.
export function ProgressRing({
  percent,
  value,
  label,
  accent = false,
  size = 132,
}: ProgressRingProps) {
  const stroke = 8
  const radius = (size - stroke) / 2
  const circumference = 2 * Math.PI * radius
  // Clamp: um percentual acima de 100 daria offset negativo e o arco vazaria.
  const safePercent = Math.max(0, Math.min(100, percent))
  const offset = circumference * (1 - safePercent / 100)

  return (
    <div
      className="relative shrink-0"
      style={{ width: size, height: size }}
      role="progressbar"
      aria-valuenow={safePercent}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-label={label}
    >
      {/*
        -90° põe o início do arco no topo, e não às 3 horas.

        overflow-visible é o que conserta o glow "quadrado": o círculo é tangente
        às quatro bordas do viewport, então o blur do drop-shadow cai fora dele —
        e o SVG raiz recorta em overflow:hidden por padrão, cortando o brilho no
        retângulo do viewport. Com overflow visível, o blur pinta para fora e
        acompanha a curva. O contêiner pai (card com padding) tem folga para
        acomodar o transbordo.
      */}
      <svg
        width={size}
        height={size}
        className="-rotate-90 overflow-visible"
        aria-hidden="true"
      >
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          strokeWidth={stroke}
          className="text-line"
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke="currentColor"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          className={[
            'transition-[stroke-dashoffset] duration-500',
            // Sem a meta batida o arco fica em cinza claro: o verde pleno é o
            // sinal de conquista, não a cor padrão do progresso. O glow só
            // acompanha o verde — cinza brilhando não significaria nada.
            accent ? 'text-brand-green glow-ring' : 'text-slate-500',
          ].join(' ')}
        />
      </svg>

      <div className="absolute inset-0 flex flex-col items-center justify-center">
        {/* Número principal da tela: sempre vívido, com glow. Verde quando a
            meta é batida; branco no resto do tempo. */}
        <span
          className={[
            'text-3xl font-semibold tabular-nums',
            accent ? 'text-brand-green glow-text' : 'text-ink glow-ink',
          ].join(' ')}
        >
          {value}
        </span>
        <span className="mt-0.5 text-[10px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
          {label}
        </span>
      </div>
    </div>
  )
}

export default ProgressRing
