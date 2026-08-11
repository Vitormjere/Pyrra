import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ChevronLeft, Check, ImagePlus, X } from 'lucide-react'
import CategoryToggleRow from '../../components/CategoryToggleRow'
import Segmented from '../../components/Segmented'
import Skeleton from '../../components/Skeleton'
import { activateTeamCategory, deactivateTeamCategory, getTeamCategories } from '../../services/challengeService'
import { createTeam, uploadTeamBannerImage } from '../../services/teamService'
import { getApiErrorMessage } from '../../services/apiError'
import { TEAM_BANNER_SWATCH, TEAM_BANNER_THEMES } from '../../utils/teamBanners'
import type { TeamCategoryStatus } from '../../types/challenges'
import type { Team, TeamBannerTheme, TeamVisibility } from '../../types/teams'

// Mesma regra do backend (TeamService.SetBannerImageAsync) — só pra dar feedback antes de
// enviar; o backend segue sendo a validação de verdade.
const MAX_BANNER_BYTES = 3 * 1024 * 1024
const ACCEPTED_BANNER_TYPES = 'image/jpeg,image/png,image/webp'

const VISIBILITY_OPTIONS: readonly TeamVisibility[] = ['Privado', 'Publico']

const VISIBILITY_LABELS: Record<TeamVisibility, string> = {
  Privado: 'Privado',
  Publico: 'Público',
}

const VISIBILITY_HINTS: Record<TeamVisibility, string> = {
  Privado: 'Só entra por convite direto ou link.',
  Publico: 'Aparece em Explorar — qualquer um entra direto.',
}

const inputClasses =
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-sm font-medium text-slate-300'

const listClasses = 'divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line'

export function CriarTime() {
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [memberLimit, setMemberLimit] = useState('10')
  const [visibility, setVisibility] = useState<TeamVisibility>('Privado')
  const [bannerTheme, setBannerTheme] = useState<TeamBannerTheme>('Verde')
  const [bannerFile, setBannerFile] = useState<File | null>(null)
  const [bannerPreviewUrl, setBannerPreviewUrl] = useState<string | null>(null)
  const [bannerFileError, setBannerFileError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Passo 2: time já criado, escolhendo categorias antes de ir pro detalhe — não-nulo só depois
  // que POST /api/times responde com sucesso.
  const [createdTeam, setCreatedTeam] = useState<Team | null>(null)
  const [categories, setCategories] = useState<TeamCategoryStatus[] | null>(null)
  const [categoryBusyId, setCategoryBusyId] = useState<string | null>(null)
  const [categoryError, setCategoryError] = useState<string | null>(null)

  // Mesma regra do backend: número positivo, sem mínimo/máximo do sistema.
  const limitValue = Number(memberLimit)
  const canSubmit = name.trim().length > 0 && Number.isInteger(limitValue) && limitValue > 0

  useEffect(() => {
    if (!createdTeam) return
    let active = true

    async function run() {
      try {
        const data = await getTeamCategories(createdTeam!.id)
        if (active) setCategories(data)
      } catch (err) {
        if (active) setCategoryError(getApiErrorMessage(err, {}, 'Não foi possível carregar as categorias.'))
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [createdTeam])

  async function handleToggleCategory(category: TeamCategoryStatus) {
    if (!createdTeam) return
    setCategoryBusyId(category.id)
    setCategoryError(null)
    try {
      if (category.isActive) {
        await deactivateTeamCategory(createdTeam.id, category.id)
      } else {
        await activateTeamCategory(createdTeam.id, category.id)
      }
      setCategories(await getTeamCategories(createdTeam.id))
    } catch (err) {
      setCategoryError(getApiErrorMessage(err, {}, 'Não foi possível atualizar a categoria.'))
    } finally {
      setCategoryBusyId(null)
    }
  }

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = '' // permite escolher o mesmo arquivo de novo depois de remover
    if (!file) return

    if (file.size > MAX_BANNER_BYTES) {
      setBannerFileError('A imagem deve ter até 3MB.')
      return
    }

    setBannerFileError(null)
    setBannerFile(file)
    setBannerPreviewUrl((current) => {
      if (current) URL.revokeObjectURL(current)
      return URL.createObjectURL(file)
    })
  }

  function handleRemoveFile() {
    if (bannerPreviewUrl) URL.revokeObjectURL(bannerPreviewUrl)
    setBannerFile(null)
    setBannerPreviewUrl(null)
    setBannerFileError(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canSubmit || saving) return

    setSaving(true)
    setError(null)
    try {
      const team = await createTeam(name.trim(), description.trim() || null, limitValue, visibility, bannerTheme)

      if (bannerFile) {
        try {
          await uploadTeamBannerImage(team.id, bannerFile)
        } catch (err) {
          // O time já existe — navega mesmo assim, só carregando o erro pra tela de detalhes
          // mostrar (dá pra tentar o upload de novo por lá). Categorias ficam pra depois, dá pra
          // ativar na tela do time a qualquer momento.
          navigate(`/times/${team.id}`, {
            replace: true,
            state: { bannerError: getApiErrorMessage(err, {}, 'Time criado, mas não foi possível enviar a imagem.') },
          })
          return
        }
      }

      // time criado com sucesso — passo 2 (categorias) antes de ir pro detalhe, em vez de navegar direto
      setCreatedTeam(team)
      setSaving(false)
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível criar o time.'))
      setSaving(false)
    }
  }

  function handleFinish() {
    if (!createdTeam) return
    navigate(`/times/${createdTeam.id}`, { replace: true })
  }

  // Passo 2: time já criado, só falta escolher categorias (ou pular) antes de ir pro time.
  if (createdTeam) {
    return (
      <div className="flex flex-col gap-5">
        <header className="flex items-center gap-2">
          <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
            {createdTeam.name}
          </h1>
        </header>

        <div className="flex flex-col gap-2">
          <span className={labelClasses}>Categorias de desafios</span>
          <p className="text-xs text-slate-500">
            Ative as categorias que valem desafio pra esse time — dá pra mudar isso a qualquer
            momento na tela do time.
          </p>
          {categories === null ? (
            <Skeleton className="h-16" />
          ) : categories.length === 0 ? (
            <p className="rounded-md bg-surface px-4 py-3 text-sm text-slate-500 ring-1 ring-line">
              Nenhuma categoria disponível no momento.
            </p>
          ) : (
            <ul className={listClasses}>
              {categories.map((category) => (
                <CategoryToggleRow
                  key={category.id}
                  category={category}
                  busy={categoryBusyId === category.id}
                  onToggle={() => handleToggleCategory(category)}
                />
              ))}
            </ul>
          )}
          {categoryError && (
            <p role="alert" className="text-xs text-red-300">
              {categoryError}
            </p>
          )}
        </div>

        <button
          type="button"
          onClick={handleFinish}
          className="w-full rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Ir para o time
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-5">
      <header className="flex items-center gap-2">
        <Link
          to="/times"
          aria-label="Voltar para Times"
          className="-ml-2 rounded-lg p-2 text-slate-400 transition hover:bg-surface hover:text-ink"
        >
          <ChevronLeft size={22} />
        </Link>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
          Criar Time
        </h1>
      </header>

      <form
        onSubmit={handleSubmit}
        className="flex flex-col gap-4 rounded-md bg-surface px-5 py-4 ring-1 ring-line"
      >
        <div className="flex flex-col gap-1.5">
          <label htmlFor="team-name" className={labelClasses}>
            Nome
          </label>
          <input
            id="team-name"
            type="text"
            value={name}
            onChange={(event) => setName(event.target.value)}
            maxLength={100}
            placeholder="Ex.: Guerreiros do Streak"
            className={inputClasses}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="team-description" className={labelClasses}>
            Descrição <span className="font-normal text-slate-500">(opcional)</span>
          </label>
          <textarea
            id="team-description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            maxLength={500}
            rows={3}
            placeholder="Sobre o que é esse time?"
            className={`${inputClasses} resize-none`}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="team-limit" className={labelClasses}>
            Limite de membros
          </label>
          <input
            id="team-limit"
            type="number"
            min={1}
            step={1}
            value={memberLimit}
            onChange={(event) => setMemberLimit(event.target.value)}
            className={inputClasses}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <span className={labelClasses}>Visibilidade</span>
          <Segmented
            label="Visibilidade do time"
            options={VISIBILITY_OPTIONS}
            value={visibility}
            onChange={setVisibility}
            labels={VISIBILITY_LABELS}
          />
          <p className="text-xs text-slate-500">{VISIBILITY_HINTS[visibility]}</p>
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

            <span className="mx-1 h-9 w-px bg-line" aria-hidden="true" />

            {bannerPreviewUrl ? (
              <div className="flex items-center gap-2">
                <img
                  src={bannerPreviewUrl}
                  alt="Prévia da imagem do banner"
                  className="h-9 w-16 rounded-md object-cover ring-1 ring-line"
                />
                <button
                  type="button"
                  onClick={handleRemoveFile}
                  aria-label="Remover imagem escolhida"
                  className="rounded-lg p-2 text-slate-400 transition hover:bg-surface-hi hover:text-red-400"
                >
                  <X size={16} />
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                className="inline-flex shrink-0 items-center gap-1.5 rounded-xl px-3 py-2 text-sm font-medium text-ink ring-1 ring-line transition hover:bg-surface-hi"
              >
                <ImagePlus size={15} aria-hidden="true" />
                Enviar imagem
              </button>
            )}

            <input
              ref={fileInputRef}
              type="file"
              accept={ACCEPTED_BANNER_TYPES}
              onChange={handleFileChange}
              className="hidden"
            />
          </div>
          <p className="text-xs text-slate-500">
            {bannerPreviewUrl
              ? 'A imagem enviada tem prioridade sobre a cor escolhida.'
              : 'Opcional — JPG, PNG ou WEBP, até 3MB. Sem imagem, usa a cor escolhida.'}
          </p>
          {bannerFileError && (
            <p role="alert" className="text-xs text-red-300">
              {bannerFileError}
            </p>
          )}
        </div>

        <button
          type="submit"
          disabled={!canSubmit || saving}
          className="w-full rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {saving ? 'Criando…' : 'Criar time'}
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

export default CriarTime
