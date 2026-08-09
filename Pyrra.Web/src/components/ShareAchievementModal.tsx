import { useEffect, useRef, useState } from 'react'
// html2canvas "clássico" não entende oklch() — e o Tailwind v4 resolve
// praticamente toda cor (paleta padrão inclusive, não só as de raridade com
// opacidade) usando color-mix(in oklab, ...). Esse fork corrige isso.
import html2canvas from 'html2canvas-pro'
import { Share2, X } from 'lucide-react'
import { classesForRarity, iconForAchievementType, RARITY_LABELS } from '../utils/achievementDisplay'
import { formatShortDate } from '../utils/format'
import type { AchievementResponse } from '../types/achievement'

interface ShareAchievementModalProps {
  achievement: AchievementResponse
  onClose: () => void
}

// abre a partir de uma conquista JÁ desbloqueada (ver AchievementCard) — o card
// renderizado aqui dentro é literalmente o que o html2canvas fotografa, então
// todo o texto/estilo dele é o que sai na imagem compartilhada
export function ShareAchievementModal({ achievement, onClose }: ShareAchievementModalProps) {
  const cardRef = useRef<HTMLDivElement>(null)
  const [generating, setGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const Icon = iconForAchievementType(achievement.type)
  const classes = classesForRarity(achievement.rarity)

  // esc fecha, mesmo padrão dos outros modais do app
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  async function handleShare() {
    if (!cardRef.current) return

    setGenerating(true)
    setError(null)

    try {
      const canvas = await html2canvas(cardRef.current, {
        // sem isso o html2canvas usa fundo branco — o card é transparente por cima do bg-surface do modal
        backgroundColor: '#05090A',
        scale: 2,
      })

      const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/png'))
      if (!blob) {
        throw new Error('canvas vazio')
      }

      const fileName = `conquista-${achievement.name.toLowerCase().replace(/\s+/g, '-')}.png`
      const file = new File([blob], fileName, { type: 'image/png' })
      const shareText = `Desbloqueei "${achievement.name}" no Pyrra! 🏆`

      if (navigator.canShare?.({ files: [file] })) {
        // celular: abre a folha de compartilhamento nativa, o usuário decide pra onde vai
        await navigator.share({ files: [file], title: achievement.name, text: shareText })
      } else {
        // desktop, sem Web Share API de arquivo: baixa o PNG, o usuário compartilha por fora
        const url = URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = url
        link.download = fileName
        link.click()
        URL.revokeObjectURL(url)
      }
    } catch (err) {
      // usuário cancelou a folha de compartilhamento nativa — não é erro de verdade
      if (err instanceof DOMException && err.name === 'AbortError') return
      setError('Não foi possível gerar a imagem. Tente novamente.')
    } finally {
      setGenerating(false)
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-brand-dark/80 p-4 backdrop-blur-sm"
      role="dialog"
      aria-modal="true"
      aria-labelledby="share-title"
    >
      <div className="w-full max-w-sm rounded-md bg-surface p-5 ring-1 ring-line">
        <div className="flex items-center justify-between">
          <h2 id="share-title" className="font-display text-lg font-semibold text-ink">
            Compartilhar
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Fechar"
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-surface-hi hover:text-ink"
          >
            <X size={18} />
          </button>
        </div>

        <div
          ref={cardRef}
          className="mt-4 flex flex-col items-center gap-2 rounded-2xl bg-brand-dark px-6 py-8 text-center ring-1 ring-line"
        >
          <span
            aria-hidden="true"
            className={`flex size-16 items-center justify-center rounded-full ring-1 ${classes.ringSoft}`}
          >
            <Icon size={30} strokeWidth={1.75} className={classes.text} />
          </span>

          {achievement.rarity && (
            <p className={`text-xs font-semibold tracking-wide uppercase ${classes.text}`}>
              {RARITY_LABELS[achievement.rarity]}
            </p>
          )}

          <h3 className={`font-display text-2xl font-semibold tracking-tight ${classes.text}`}>
            {achievement.name}
          </h3>

          <p className="text-sm text-slate-400">{achievement.description}</p>

          <p className="text-sm font-semibold text-slate-200 tabular-nums">+{achievement.xp} XP</p>

          {achievement.unlockedAt && (
            <p className="text-xs text-slate-600">
              Desbloqueado em {formatShortDate(achievement.unlockedAt.slice(0, 10))}
            </p>
          )}

          <p className="mt-2 text-[11px] font-semibold tracking-widest text-brand-green">PYRRA</p>
        </div>

        {error && (
          <p role="alert" className="mt-3 text-sm text-red-300">
            {error}
          </p>
        )}

        <button
          type="button"
          onClick={handleShare}
          disabled={generating}
          className="mt-4 flex w-full items-center justify-center gap-2 rounded-xl bg-brand-green px-4 py-3 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Share2 size={16} aria-hidden="true" />
          {generating ? 'Gerando imagem...' : 'Compartilhar'}
        </button>
      </div>
    </div>
  )
}

export default ShareAchievementModal
