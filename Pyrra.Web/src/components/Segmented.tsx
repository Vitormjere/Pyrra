interface SegmentedProps<T extends string> {
  options: readonly T[]
  value: T
  onChange: (value: T) => void
  /** Rótulo visível por opção — separa o texto com acento do valor da API. */
  labels?: Partial<Record<T, string>>
  /** Cor do item selecionado, por opção (ex.: faixas de prioridade). */
  activeColors?: Partial<Record<T, string>>
  label: string
}

// Seletor segmentado: um toque para escolher, contra dois de um select nativo
// (abrir + selecionar). É a diferença que importa no celular.
export function Segmented<T extends string>({
  options,
  value,
  onChange,
  labels,
  activeColors,
  label,
}: SegmentedProps<T>) {
  return (
    <fieldset>
      <legend className="sr-only">{label}</legend>
      <div className="flex gap-1 rounded-xl bg-white/5 p-1">
        {options.map((option) => {
          const active = option === value
          return (
            <button
              key={option}
              type="button"
              aria-pressed={active}
              onClick={() => onChange(option)}
              className={[
                'flex-1 rounded-lg px-2 py-2 text-xs font-medium transition',
                active
                  ? `bg-white/10 ${activeColors?.[option] ?? 'text-slate-100'}`
                  : 'text-slate-400 hover:text-slate-200',
              ].join(' ')}
            >
              {labels?.[option] ?? option}
            </button>
          )
        })}
      </div>
    </fieldset>
  )
}

export default Segmented
