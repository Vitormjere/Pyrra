import { useEffect, useRef, useState } from 'react'
import SectionHeader from './SectionHeader'
import { getToday, save } from '../services/planningService'
import { getApiErrorMessage } from '../services/apiError'

type SaveState = 'idle' | 'saving' | 'saved' | 'error'

// carrega os próprios dados em vez de depender da tela Hoje, assim uma falha aqui não atrapalha o resto
export function ReflectionCard() {
  const [content, setContent] = useState('')
  const [loading, setLoading] = useState(true)
  const [state, setState] = useState<SaveState>('idle')
  const [error, setError] = useState<string | null>(null)
  // última versão confirmada pelo servidor, comparar evita PUT quando o usuário só entra e sai do campo
  const savedContent = useRef('')

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const note = await getToday()
        if (!active) return
        setContent(note.content)
        savedContent.current = note.content
      } catch {
        // silencioso: o card some em vez de exibir erro, é conteúdo opcional
        if (active) setError('unavailable')
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  // salva no blur, não a cada tecla — autosave por digitação geraria uma requisição por palavra
  async function handleBlur() {
    if (content === savedContent.current) return

    setState('saving')
    setError(null)

    try {
      const note = await save(content)
      savedContent.current = note.content
      setState('saved')
    } catch (err) {
      setState('error')
      setError(getApiErrorMessage(err, {}, 'Não foi possível salvar.'))
    }
  }

  if (loading || error === 'unavailable') return null

  return (
    <section className="flex flex-col gap-2">
      <SectionHeader
        trailing={
          <span
            aria-live="polite"
            className={[
              'text-[11px]',
              state === 'error' ? 'text-red-300' : 'text-slate-500',
            ].join(' ')}
          >
            {state === 'saving' && 'Salvando...'}
            {state === 'saved' && 'Salvo'}
            {state === 'error' && (error ?? 'Erro ao salvar')}
          </span>
        }
      >
        Reflexão do dia
      </SectionHeader>

      <textarea
        value={content}
        onChange={(event) => {
          setContent(event.target.value)
          // limpa o "Salvo" anterior, mantê-lo enquanto o texto muda afirmaria algo que não é mais verdade
          if (state !== 'idle') setState('idle')
        }}
        onBlur={handleBlur}
        rows={4}
        placeholder="Como foi o dia? O que ficou na cabeça?"
        aria-label="Reflexão do dia"
        className="w-full resize-y rounded-md bg-surface px-4 py-3 text-sm leading-relaxed text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green"
      />
    </section>
  )
}

export default ReflectionCard
