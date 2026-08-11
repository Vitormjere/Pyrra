import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Check, ChevronDown, Pencil, Plus, Trash2 } from 'lucide-react'
import EmptyState from '../../../components/EmptyState'
import Skeleton from '../../../components/Skeleton'
import { useConfirm } from '../../../hooks/useConfirm'
import {
  createCategory,
  createChallenge,
  deleteCategory,
  deleteChallenge,
  getCategories,
  getChallenges,
  updateCategory,
  updateChallenge,
} from '../../../services/adminChallengeCatalogService'
import { getApiErrorMessage } from '../../../services/apiError'
import { CATEGORY_ICON_KEYS, getCategoryIcon } from '../../../utils/challengeCategoryIcons'
import { CHALLENGE_CATEGORY_COLORS, CHALLENGE_CATEGORY_SWATCH } from '../../../utils/challengeCategoryColors'
import type { ChallengeCategory, ChallengeCategoryColor } from '../../../types/challenges'
import type { AdminChallenge } from '../../../types/challengeCatalog'

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-xs font-medium text-slate-400'

const primaryButtonClasses =
  'flex-1 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60'

const secondaryButtonClasses =
  'rounded-xl px-3 py-2 text-sm font-medium text-slate-400 ring-1 ring-line transition hover:bg-surface-hi'

interface CategoryDraft {
  name: string
  description: string
  icon: string
  color: ChallengeCategoryColor
}

const emptyCategoryDraft: CategoryDraft = { name: '', description: '', icon: CATEGORY_ICON_KEYS[0], color: 'Verde' }

// campos compartilhados entre criar e editar categoria
function CategoryFormFields({
  draft,
  onChange,
}: {
  draft: CategoryDraft
  onChange: (next: CategoryDraft) => void
}) {
  return (
    <>
      <div className="flex flex-col gap-1">
        <label className={labelClasses}>Nome</label>
        <input
          type="text"
          value={draft.name}
          onChange={(event) => onChange({ ...draft, name: event.target.value })}
          maxLength={100}
          placeholder="Ex.: Corrida"
          className={inputClasses}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className={labelClasses}>
          Descrição <span className="font-normal text-slate-500">(opcional)</span>
        </label>
        <input
          type="text"
          value={draft.description}
          onChange={(event) => onChange({ ...draft, description: event.target.value })}
          maxLength={500}
          placeholder="Uma linha sobre a categoria"
          className={inputClasses}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className={labelClasses}>Ícone</label>
        <select
          value={draft.icon}
          onChange={(event) => onChange({ ...draft, icon: event.target.value })}
          className={inputClasses}
        >
          {CATEGORY_ICON_KEYS.map((key) => (
            <option key={key} value={key}>
              {key}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1.5">
        <span className={labelClasses}>Cor</span>
        <div className="flex flex-wrap gap-2">
          {CHALLENGE_CATEGORY_COLORS.map((color) => (
            <button
              key={color}
              type="button"
              aria-label={color}
              aria-pressed={draft.color === color}
              onClick={() => onChange({ ...draft, color })}
              className={[
                'flex size-8 shrink-0 items-center justify-center rounded-full ring-2 transition',
                CHALLENGE_CATEGORY_SWATCH[color],
                draft.color === color ? 'ring-ink' : 'ring-transparent',
              ].join(' ')}
            >
              {draft.color === color && <Check size={14} className="text-brand-dark" />}
            </button>
          ))}
        </div>
      </div>
    </>
  )
}

interface ChallengeDraft {
  title: string
  description: string
  points: string
  /** yyyy-mm-dd de um <input type="date">; convertido pro fim do dia ao enviar. */
  deadline: string
}

const emptyChallengeDraft: ChallengeDraft = { title: '', description: '', points: '10', deadline: '' }

function draftFromChallenge(challenge: AdminChallenge): ChallengeDraft {
  return {
    title: challenge.title,
    description: challenge.description ?? '',
    points: String(challenge.points),
    deadline: challenge.deadline ? challenge.deadline.slice(0, 10) : '',
  }
}

// "válido até o fim desse dia", não a meia-noite de início — mais intuitivo pra quem escolhe a data
function deadlineToIso(deadline: string): string | null {
  return deadline ? new Date(`${deadline}T23:59:59`).toISOString() : null
}

function parsePoints(value: string): number | null {
  const points = Number(value)
  return Number.isInteger(points) && points > 0 ? points : null
}

function ChallengeFormFields({
  draft,
  onChange,
}: {
  draft: ChallengeDraft
  onChange: (next: ChallengeDraft) => void
}) {
  return (
    <>
      <input
        type="text"
        value={draft.title}
        onChange={(event) => onChange({ ...draft, title: event.target.value })}
        maxLength={200}
        placeholder="Título do desafio"
        aria-label="Título do desafio"
        className={inputClasses}
      />
      <input
        type="text"
        value={draft.description}
        onChange={(event) => onChange({ ...draft, description: event.target.value })}
        maxLength={1000}
        placeholder="Descrição (opcional)"
        aria-label="Descrição do desafio"
        className={inputClasses}
      />
      <div className="flex gap-2">
        <input
          type="number"
          inputMode="numeric"
          min="1"
          value={draft.points}
          onChange={(event) => onChange({ ...draft, points: event.target.value })}
          placeholder="Pontos"
          aria-label="Pontos"
          className={inputClasses}
        />
        <input
          type="date"
          value={draft.deadline}
          onChange={(event) => onChange({ ...draft, deadline: event.target.value })}
          aria-label="Prazo final (opcional)"
          className={inputClasses}
        />
      </div>
    </>
  )
}

function ChallengesPanel({
  categoryId,
  challenges,
  loading,
  error,
  onChanged,
}: {
  categoryId: string
  challenges: AdminChallenge[] | undefined
  loading: boolean
  error: string | null
  onChanged: (challenges: AdminChallenge[]) => void
}) {
  const { confirm, dialog } = useConfirm()

  const [addingOpen, setAddingOpen] = useState(false)
  const [addDraft, setAddDraft] = useState<ChallengeDraft>(emptyChallengeDraft)
  const [addBusy, setAddBusy] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editDraft, setEditDraft] = useState<ChallengeDraft>(emptyChallengeDraft)
  const [editBusy, setEditBusy] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  async function refresh() {
    onChanged(await getChallenges(categoryId))
  }

  async function handleAdd(event: FormEvent) {
    event.preventDefault()
    const points = parsePoints(addDraft.points)
    if (!addDraft.title.trim() || points === null || addBusy) return

    setAddBusy(true)
    setAddError(null)
    try {
      await createChallenge({
        categoryId,
        title: addDraft.title.trim(),
        description: addDraft.description.trim() || null,
        points,
        deadline: deadlineToIso(addDraft.deadline),
      })
      setAddDraft(emptyChallengeDraft)
      setAddingOpen(false)
      await refresh()
    } catch (err) {
      setAddError(getApiErrorMessage(err, {}, 'Não foi possível criar o desafio.'))
    } finally {
      setAddBusy(false)
    }
  }

  function startEdit(challenge: AdminChallenge) {
    setEditingId(challenge.id)
    setEditDraft(draftFromChallenge(challenge))
    setEditError(null)
  }

  async function handleUpdate(event: FormEvent, challengeId: string) {
    event.preventDefault()
    const points = parsePoints(editDraft.points)
    if (!editDraft.title.trim() || points === null || editBusy) return

    setEditBusy(true)
    setEditError(null)
    try {
      await updateChallenge(challengeId, {
        categoryId,
        title: editDraft.title.trim(),
        description: editDraft.description.trim() || null,
        points,
        deadline: deadlineToIso(editDraft.deadline),
      })
      setEditingId(null)
      await refresh()
    } catch (err) {
      setEditError(getApiErrorMessage(err, {}, 'Não foi possível salvar o desafio.'))
    } finally {
      setEditBusy(false)
    }
  }

  async function handleDelete(challenge: AdminChallenge) {
    const ok = await confirm({
      title: 'Excluir desafio',
      message: `Excluir "${challenge.title}"? Essa ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      destructive: true,
    })
    if (!ok) return

    setDeletingId(challenge.id)
    setDeleteError(null)
    try {
      await deleteChallenge(challenge.id)
      await refresh()
    } catch (err) {
      setDeleteError(getApiErrorMessage(err, {}, 'Não foi possível excluir o desafio.'))
    } finally {
      setDeletingId(null)
    }
  }

  return (
    <div className="mt-2 flex flex-col gap-2 border-t border-line pt-3 pl-11">
      {loading && <Skeleton className="h-10" />}
      {error && (
        <p role="alert" className="text-xs text-red-300">
          {error}
        </p>
      )}
      {deleteError && (
        <p role="alert" className="text-xs text-red-300">
          {deleteError}
        </p>
      )}

      {!loading && challenges && challenges.length === 0 && (
        <p className="text-xs text-slate-500">Nenhum desafio nessa categoria ainda.</p>
      )}

      {!loading && challenges && challenges.length > 0 && (
        <ul className="flex flex-col gap-1">
          {challenges.map((challenge) =>
            editingId === challenge.id ? (
              <li key={challenge.id}>
                <form
                  onSubmit={(event) => handleUpdate(event, challenge.id)}
                  className="flex flex-col gap-2 rounded-md bg-surface-hi p-2 ring-1 ring-line"
                >
                  <ChallengeFormFields draft={editDraft} onChange={setEditDraft} />
                  <div className="flex gap-2">
                    <button type="submit" disabled={editBusy} className={primaryButtonClasses}>
                      {editBusy ? 'Salvando…' : 'Salvar'}
                    </button>
                    <button type="button" onClick={() => setEditingId(null)} className={secondaryButtonClasses}>
                      Cancelar
                    </button>
                  </div>
                  {editError && (
                    <p role="alert" className="text-xs text-red-300">
                      {editError}
                    </p>
                  )}
                </form>
              </li>
            ) : (
              <li
                key={challenge.id}
                className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm transition hover:bg-surface-hi"
              >
                <span className="min-w-0 flex-1 truncate text-ink">{challenge.title}</span>
                <span className="shrink-0 text-xs text-slate-500 tabular-nums">+{challenge.points} pts</span>
                {challenge.deadline && (
                  <span className="shrink-0 rounded-full bg-amber-500/10 px-1.5 py-0.5 text-[10px] font-medium text-amber-400 ring-1 ring-amber-500/20">
                    até {new Date(challenge.deadline).toLocaleDateString('pt-BR')}
                  </span>
                )}
                <button
                  type="button"
                  onClick={() => startEdit(challenge)}
                  aria-label={`Editar ${challenge.title}`}
                  className="shrink-0 rounded p-1 text-slate-500 transition hover:text-brand-green"
                >
                  <Pencil size={13} />
                </button>
                <button
                  type="button"
                  disabled={deletingId === challenge.id}
                  onClick={() => handleDelete(challenge)}
                  aria-label={`Excluir ${challenge.title}`}
                  className="shrink-0 rounded p-1 text-slate-500 transition hover:text-red-400 disabled:opacity-50"
                >
                  <Trash2 size={13} />
                </button>
              </li>
            ),
          )}
        </ul>
      )}

      {addingOpen ? (
        <form onSubmit={handleAdd} className="flex flex-col gap-2 rounded-md bg-surface-hi p-2 ring-1 ring-line">
          <ChallengeFormFields draft={addDraft} onChange={setAddDraft} />
          <div className="flex gap-2">
            <button type="submit" disabled={addBusy} className={primaryButtonClasses}>
              {addBusy ? 'Adicionando…' : 'Adicionar'}
            </button>
            <button
              type="button"
              onClick={() => {
                setAddingOpen(false)
                setAddDraft(emptyChallengeDraft)
                setAddError(null)
              }}
              className={secondaryButtonClasses}
            >
              Cancelar
            </button>
          </div>
          {addError && (
            <p role="alert" className="text-xs text-red-300">
              {addError}
            </p>
          )}
        </form>
      ) : (
        <button
          type="button"
          onClick={() => setAddingOpen(true)}
          className="inline-flex shrink-0 items-center gap-1.5 self-start rounded-lg px-2.5 py-1.5 text-xs font-medium text-brand-green ring-1 ring-brand-green/30 transition hover:bg-brand-green/10"
        >
          <Plus size={13} aria-hidden="true" />
          Adicionar desafio
        </button>
      )}

      {dialog}
    </div>
  )
}

// gestão do catálogo de categorias e desafios (só admin, backend já existe — ChallengeCatalogController)
export function AdminDesafios() {
  const { confirm, dialog } = useConfirm()

  const [categories, setCategories] = useState<ChallengeCategory[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [creatingCategory, setCreatingCategory] = useState(false)
  const [newCategory, setNewCategory] = useState<CategoryDraft>(emptyCategoryDraft)
  const [createBusy, setCreateBusy] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)

  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null)
  const [editDraft, setEditDraft] = useState<CategoryDraft>(emptyCategoryDraft)
  const [editBusy, setEditBusy] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  const [deletingCategoryId, setDeletingCategoryId] = useState<string | null>(null)

  const [expandedCategoryId, setExpandedCategoryId] = useState<string | null>(null)
  const [challengesByCategory, setChallengesByCategory] = useState<Record<string, AdminChallenge[]>>({})
  const [loadingChallengesFor, setLoadingChallengesFor] = useState<string | null>(null)
  const [challengesError, setChallengesError] = useState<string | null>(null)

  useEffect(() => {
    void loadCategories()
  }, [])

  async function loadCategories() {
    try {
      setCategories(await getCategories())
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível carregar as categorias.'))
    }
  }

  async function handleCreateCategory(event: FormEvent) {
    event.preventDefault()
    if (!newCategory.name.trim() || createBusy) return

    setCreateBusy(true)
    setCreateError(null)
    try {
      await createCategory({
        name: newCategory.name.trim(),
        description: newCategory.description.trim() || null,
        icon: newCategory.icon,
        color: newCategory.color,
      })
      setNewCategory(emptyCategoryDraft)
      setCreatingCategory(false)
      await loadCategories()
    } catch (err) {
      setCreateError(getApiErrorMessage(err, {}, 'Não foi possível criar a categoria.'))
    } finally {
      setCreateBusy(false)
    }
  }

  function startEditCategory(category: ChallengeCategory) {
    setEditingCategoryId(category.id)
    setEditDraft({
      name: category.name,
      description: category.description ?? '',
      icon: category.icon,
      color: category.color,
    })
    setEditError(null)
  }

  async function handleUpdateCategory(event: FormEvent, categoryId: string) {
    event.preventDefault()
    if (!editDraft.name.trim() || editBusy) return

    setEditBusy(true)
    setEditError(null)
    try {
      await updateCategory(categoryId, {
        name: editDraft.name.trim(),
        description: editDraft.description.trim() || null,
        icon: editDraft.icon,
        color: editDraft.color,
      })
      setEditingCategoryId(null)
      await loadCategories()
    } catch (err) {
      setEditError(getApiErrorMessage(err, {}, 'Não foi possível salvar a categoria.'))
    } finally {
      setEditBusy(false)
    }
  }

  async function handleDeleteCategory(category: ChallengeCategory) {
    const ok = await confirm({
      title: 'Excluir categoria',
      message: `Excluir "${category.name}"? Só é possível se não houver desafios cadastrados nela.`,
      confirmLabel: 'Excluir',
      destructive: true,
    })
    if (!ok) return

    setDeletingCategoryId(category.id)
    setError(null)
    try {
      await deleteCategory(category.id)
      await loadCategories()
    } catch (err) {
      setError(
        getApiErrorMessage(
          err,
          { 409: `"${category.name}" ainda tem desafios cadastrados — remova-os antes de excluir a categoria.` },
          'Não foi possível excluir a categoria.',
        ),
      )
    } finally {
      setDeletingCategoryId(null)
    }
  }

  async function toggleExpand(categoryId: string) {
    if (expandedCategoryId === categoryId) {
      setExpandedCategoryId(null)
      return
    }

    setExpandedCategoryId(categoryId)
    if (challengesByCategory[categoryId]) return

    setLoadingChallengesFor(categoryId)
    setChallengesError(null)
    try {
      const challenges = await getChallenges(categoryId)
      setChallengesByCategory((current) => ({ ...current, [categoryId]: challenges }))
    } catch (err) {
      setChallengesError(getApiErrorMessage(err, {}, 'Não foi possível carregar os desafios.'))
    } finally {
      setLoadingChallengesFor(null)
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Desafios</h1>

      {error && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}

      {/* NOVA CATEGORIA */}
      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">Nova categoria</h2>
        {creatingCategory ? (
          <form
            onSubmit={handleCreateCategory}
            className="flex flex-col gap-2 rounded-md bg-surface px-4 py-3 ring-1 ring-line"
          >
            <CategoryFormFields draft={newCategory} onChange={setNewCategory} />
            <div className="flex gap-2">
              <button type="submit" disabled={createBusy} className={primaryButtonClasses}>
                {createBusy ? 'Criando…' : 'Criar categoria'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setCreatingCategory(false)
                  setNewCategory(emptyCategoryDraft)
                  setCreateError(null)
                }}
                className={secondaryButtonClasses}
              >
                Cancelar
              </button>
            </div>
            {createError && (
              <p role="alert" className="text-xs text-red-300">
                {createError}
              </p>
            )}
          </form>
        ) : (
          <button
            type="button"
            onClick={() => setCreatingCategory(true)}
            className="inline-flex items-center gap-1.5 self-start rounded-xl px-3 py-2 text-sm font-medium text-ink ring-1 ring-line transition hover:bg-surface-hi"
          >
            <Plus size={15} aria-hidden="true" />
            Nova categoria
          </button>
        )}
      </section>

      {/* CATEGORIAS */}
      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">Categorias</h2>

        {categories === null ? (
          <Skeleton className="h-16" />
        ) : categories.length === 0 ? (
          <EmptyState title="Nenhuma categoria cadastrada ainda." />
        ) : (
          <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
            {categories.map((category) => {
              const Icon = getCategoryIcon(category.icon)
              const expanded = expandedCategoryId === category.id

              return (
                <li key={category.id} className="px-4 py-3">
                  {editingCategoryId === category.id ? (
                    <form
                      onSubmit={(event) => handleUpdateCategory(event, category.id)}
                      className="flex flex-col gap-2"
                    >
                      <CategoryFormFields draft={editDraft} onChange={setEditDraft} />
                      <div className="flex gap-2">
                        <button type="submit" disabled={editBusy} className={primaryButtonClasses}>
                          {editBusy ? 'Salvando…' : 'Salvar'}
                        </button>
                        <button
                          type="button"
                          onClick={() => setEditingCategoryId(null)}
                          className={secondaryButtonClasses}
                        >
                          Cancelar
                        </button>
                      </div>
                      {editError && (
                        <p role="alert" className="text-xs text-red-300">
                          {editError}
                        </p>
                      )}
                    </form>
                  ) : (
                    <div className="flex items-center gap-3">
                      <span
                        aria-hidden="true"
                        className={[
                          'flex size-8 shrink-0 items-center justify-center rounded-full text-brand-dark',
                          CHALLENGE_CATEGORY_SWATCH[category.color],
                        ].join(' ')}
                      >
                        <Icon size={15} />
                      </span>
                      <div className="min-w-0 flex-1">
                        <p className="truncate font-medium text-ink">{category.name}</p>
                        {category.description && (
                          <p className="truncate text-xs text-slate-500">{category.description}</p>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => toggleExpand(category.id)}
                        aria-expanded={expanded}
                        aria-label={`Desafios de ${category.name}`}
                        className="shrink-0 rounded p-1.5 text-slate-500 transition hover:bg-surface-hi hover:text-brand-green"
                      >
                        <ChevronDown
                          size={16}
                          className={['transition-transform', expanded ? 'rotate-180' : ''].join(' ')}
                        />
                      </button>
                      <button
                        type="button"
                        onClick={() => startEditCategory(category)}
                        aria-label={`Editar ${category.name}`}
                        className="shrink-0 rounded p-1.5 text-slate-500 transition hover:bg-surface-hi hover:text-brand-green"
                      >
                        <Pencil size={14} />
                      </button>
                      <button
                        type="button"
                        disabled={deletingCategoryId === category.id}
                        onClick={() => handleDeleteCategory(category)}
                        aria-label={`Excluir ${category.name}`}
                        className="shrink-0 rounded p-1.5 text-slate-500 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  )}

                  {expanded && (
                    <ChallengesPanel
                      categoryId={category.id}
                      challenges={challengesByCategory[category.id]}
                      loading={loadingChallengesFor === category.id}
                      error={challengesError}
                      onChanged={(challenges) =>
                        setChallengesByCategory((current) => ({ ...current, [category.id]: challenges }))
                      }
                    />
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </section>

      {dialog}
    </div>
  )
}

export default AdminDesafios
