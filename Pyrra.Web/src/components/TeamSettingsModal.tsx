import { useEffect, useRef } from 'react'
import type { ChangeEvent } from 'react'
import { Check, ImagePlus, Trash2, X } from 'lucide-react'
import CategoryToggleRow from './CategoryToggleRow'
import Segmented from './Segmented'
import Skeleton from './Skeleton'
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../utils/teamBanners'
import type { TeamCategoryStatus } from '../types/challenges'
import type { Team, TeamBannerTheme, TeamVisibility } from '../types/teams'

const ACCEPTED_BANNER_TYPES = 'image/jpeg,image/png,image/webp'

const VISIBILITY_OPTIONS: readonly TeamVisibility[] = ['Privado', 'Publico']

const VISIBILITY_LABELS: Record<TeamVisibility, string> = {
  Privado: 'Privado',
  Publico: 'Público',
}

const listClasses = 'divide-y divide-line overflow-hidden rounded-md bg-surface-hi ring-1 ring-line'

interface TeamSettingsModalProps {
  team: Team
  categories: TeamCategoryStatus[] | null
  categoryBusyId: string | null
  onToggleCategory: (category: TeamCategoryStatus) => void
  bannerBusy: boolean
  bannerFileError: string | null
  onVisibilityChange: (visibility: TeamVisibility) => void
  onBannerThemeChange: (theme: TeamBannerTheme) => void
  onBannerFileChange: (event: ChangeEvent<HTMLInputElement>) => void
  onRemoveBannerImage: () => void
  onDelete: () => void
  onClose: () => void
}

// Só o dono abre (botão "Configurações do time" na tela de detalhe). Reúne o que antes ficava
// solto na página: banner/cor, visibilidade, categorias ativas e excluir time — mesmo padrão
// visual de modal do resto do app (fundo desfocado, X pra fechar, ver ZeloPlanModal/ShareAchievementModal).
export function TeamSettingsModal({
  team,
  categories,
  categoryBusyId,
  onToggleCategory,
  bannerBusy,
  bannerFileError,
  onVisibilityChange,
  onBannerThemeChange,
  onBannerFileChange,
  onRemoveBannerImage,
  onDelete,
  onClose,
}: TeamSettingsModalProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <button
        type="button"
        aria-label="Fechar"
        onClick={onClose}
        className="absolute inset-0 bg-brand-dark/80 backdrop-blur-sm"
      />

      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="team-settings-title"
        className="relative flex max-h-[85vh] w-full max-w-lg flex-col rounded-md bg-surface ring-1 ring-line"
      >
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <h2 id="team-settings-title" className="font-display text-xl font-semibold tracking-tight text-ink">
            Configurações do time
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Fechar"
            className="shrink-0 rounded p-1 text-slate-500 transition hover:bg-surface-hi hover:text-ink"
          >
            <X size={20} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-5 py-4">
          <div className="flex flex-col gap-5">
            <section className="flex flex-col gap-1.5">
              <span className="text-sm font-medium text-ink">Visibilidade</span>
              <Segmented
                label="Visibilidade do time"
                options={VISIBILITY_OPTIONS}
                value={team.visibility}
                onChange={onVisibilityChange}
                labels={VISIBILITY_LABELS}
              />
              <p className="text-xs text-slate-400">
                {team.visibility === 'Publico'
                  ? 'Aparece na aba Explorar — qualquer um entra direto.'
                  : 'Só entra por convite direto ou link.'}
              </p>
            </section>

            <section className="flex flex-col gap-2">
              <span className="text-sm font-medium text-ink">Banner</span>

              <div className="flex flex-wrap items-center gap-2">
                {TEAM_BANNER_THEMES.map((theme) => (
                  <button
                    key={theme}
                    type="button"
                    aria-label={theme}
                    aria-pressed={team.bannerTheme === theme}
                    disabled={bannerBusy}
                    onClick={() => onBannerThemeChange(theme)}
                    className={[
                      'flex size-9 shrink-0 items-center justify-center rounded-full ring-2 transition disabled:opacity-50',
                      TEAM_BANNER_SWATCH[theme],
                      team.bannerTheme === theme ? 'ring-ink' : 'ring-transparent',
                    ].join(' ')}
                  >
                    {team.bannerTheme === theme && <Check size={16} className="text-brand-dark" />}
                  </button>
                ))}

                <span className="mx-1 h-9 w-px bg-line" aria-hidden="true" />

                <button
                  type="button"
                  disabled={bannerBusy}
                  onClick={() => fileInputRef.current?.click()}
                  className="inline-flex shrink-0 items-center gap-1.5 rounded-xl px-3 py-2 text-sm font-medium text-ink ring-1 ring-line transition hover:bg-surface-hi disabled:opacity-50"
                >
                  <ImagePlus size={15} aria-hidden="true" />
                  {bannerBusy ? 'Enviando…' : 'Enviar imagem'}
                </button>

                {team.bannerImageUrl && (
                  <button
                    type="button"
                    disabled={bannerBusy}
                    onClick={onRemoveBannerImage}
                    className="inline-flex shrink-0 items-center gap-1.5 rounded-xl p-2 text-slate-400 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
                    aria-label="Remover imagem do banner"
                  >
                    <X size={15} />
                  </button>
                )}

                <input
                  ref={fileInputRef}
                  type="file"
                  accept={ACCEPTED_BANNER_TYPES}
                  onChange={onBannerFileChange}
                  className="hidden"
                />
              </div>

              <p className="text-xs text-slate-400">
                {team.bannerImageUrl
                  ? 'A imagem personalizada tem prioridade sobre a cor — remova a imagem para usar a cor escolhida.'
                  : 'Opcional — JPG, PNG ou WEBP, até 3MB.'}
              </p>
              {bannerFileError && (
                <p role="alert" className="text-xs text-red-300">
                  {bannerFileError}
                </p>
              )}
            </section>

            <section className="flex flex-col gap-2">
              <span className="text-sm font-medium text-ink">Categorias ativas</span>
              {categories === null ? (
                <Skeleton className="h-16" />
              ) : categories.length === 0 ? (
                <p className="rounded-md bg-surface-hi px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
                  Nenhuma categoria disponível no momento.
                </p>
              ) : (
                <ul className={listClasses}>
                  {categories.map((category) => (
                    <CategoryToggleRow
                      key={category.id}
                      category={category}
                      busy={categoryBusyId === category.id}
                      onToggle={() => onToggleCategory(category)}
                    />
                  ))}
                </ul>
              )}
            </section>

            <section className="border-t border-line pt-4">
              <button
                type="button"
                onClick={onDelete}
                className="inline-flex w-full items-center justify-center gap-1.5 rounded-xl px-4 py-2.5 text-sm font-semibold text-red-400 ring-1 ring-red-500/20 transition hover:bg-red-500/10"
              >
                <Trash2 size={16} aria-hidden="true" />
                Excluir time
              </button>
            </section>
          </div>
        </div>
      </div>
    </div>
  )
}

export default TeamSettingsModal
