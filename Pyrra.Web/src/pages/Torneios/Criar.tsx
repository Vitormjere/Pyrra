import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ChevronLeft, Check } from 'lucide-react'
import { createTournament } from '../../services/tournamentService'
import { getApiErrorMessage } from '../../services/apiError'
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../../utils/teamBanners'
import type { TeamBannerTheme } from '../../types/teams'

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-sm font-medium text-slate-300'

// Criação direta — só admin (POST /api/torneios). Ao contrário de Solicitar.tsx, o torneio já
// nasce criado (quem chama vira o dono na hora), por isso navega pra tela de Detalhes ao final,
// mesmo padrão de CriarTime.tsx.
export function CriarTorneio() {
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [bannerTheme, setBannerTheme] = useState<TeamBannerTheme>('Verde')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const canSubmit = name.trim().length > 0

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      const tournament = await createTournament(name.trim(), description.trim() || null, bannerTheme)
      navigate(`/torneios/${tournament.id}`, { replace: true })
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível criar o torneio.'))
      setSaving(false)
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center gap-2">
        <Link
          to="/torneios"
          aria-label="Voltar para Torneios"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Criar Torneio
        </h1>
      </header>

      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-4 rounded-md bg-surface px-5 py-4 ring-1 ring-line"
      >
        <p className="text-xs text-slate-400">Criação direta — o torneio já nasce criado, e você vira o dono.</p>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="tournament-create-name" className={labelClasses}>
            Nome
          </label>
          <input
            id="tournament-create-name"
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            maxLength={100}
            placeholder="Ex.: Copa Verão Pyrra"
            className={inputClasses}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="tournament-create-description" className={labelClasses}>
            Descrição <span className="font-normal text-slate-500">(opcional)</span>
          </label>
          <textarea
            id="tournament-create-description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            maxLength={500}
            rows={3}
            placeholder="Sobre o que é esse torneio?"
            className={`${inputClasses} resize-none`}
          />
        </div>

        <div className="flex flex-col gap-2">
          <span className={labelClasses}>Cor do banner</span>
          <div className="flex flex-wrap items-center gap-2">
            {TEAM_BANNER_THEMES.map((theme) => (
              <button
                key={theme}
                type="button"
                aria-label={theme}
                aria-pressed={bannerTheme === theme}
                onClick={() => setBannerTheme(theme)}
                className={[
                  'flex size-9 shrink-0 items-center justify-center rounded-full ring-2 transition',
                  TEAM_BANNER_SWATCH[theme],
                  bannerTheme === theme ? 'ring-ink' : 'ring-transparent',
                ].join(' ')}
              >
                {bannerTheme === theme && <Check size={16} className="text-brand-dark" />}
              </button>
            ))}
          </div>
          <p className="text-xs text-slate-500">A imagem de capa pode ser enviada depois, na tela de Detalhes.</p>
        </div>

        <button
          type="submit"
          disabled={!canSubmit || saving}
          className="w-full rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? 'Criando…' : 'Criar torneio'}
        </button>

        {error && (
          <p role="alert" className="text-center text-xs text-red-300">
            {error}
          </p>
        )}
      </form>
    </div>
  )
}

export default CriarTorneio
