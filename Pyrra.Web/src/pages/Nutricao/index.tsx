import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Plus } from 'lucide-react'
import SectionHeader from '../../components/SectionHeader'
import ItemActions from '../../components/ItemActions'
import NutritionPlanSection from '../../components/NutritionPlanSection'
import {
  addItem,
  getForDay,
  getForWeek,
  removeItem,
  updateItem,
} from '../../services/nutritionService'
import { getApiErrorMessage } from '../../services/apiError'
import { formatWeekdayShort } from '../../utils/format'
import type {
  DayNutritionResponse,
  MealType,
  WeekNutritionResponse,
} from '../../types/nutrition'

const MEAL_LABELS: Record<MealType, string> = {
  CafeDaManha: 'Café da manhã',
  Almoco: 'Almoço',
  Lanche: 'Lanche',
  Jantar: 'Jantar',
}

// Abreviações para a grade semanal, onde não cabe o nome inteiro.
const MEAL_SHORT_LABELS: Record<MealType, string> = {
  CafeDaManha: 'Café',
  Almoco: 'Almoço',
  Lanche: 'Lanche',
  Jantar: 'Jantar',
}

const MEAL_ORDER: readonly MealType[] = [
  'CafeDaManha',
  'Almoco',
  'Lanche',
  'Jantar',
]

const inputClasses =
  'w-full rounded-md bg-surface px-3 py-2.5 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-10 animate-pulse rounded-md bg-surface" />
      <div className="h-28 animate-pulse rounded-md bg-surface" />
      <div className="h-28 animate-pulse rounded-md bg-surface" />
    </div>
  )
}

type Tab = 'hoje' | 'semana' | 'plano'

export function Nutricao() {
  const [day, setDay] = useState<DayNutritionResponse | null>(null)
  const [week, setWeek] = useState<WeekNutritionResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>('hoje')

  // Qual refeição está com o formulário aberto — só uma por vez, para a tela não
  // virar quatro formulários empilhados.
  const [openMeal, setOpenMeal] = useState<MealType | null>(null)
  const [itemName, setItemName] = useState('')
  const [quantity, setQuantity] = useState('')
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [removingId, setRemovingId] = useState<string | null>(null)
  const itemInputRef = useRef<HTMLInputElement>(null)

  // Edição inline de um item já registrado — só um por vez, como o formulário de
  // adição. Guarda os campos e a trava de "salvando".
  const [editingItemId, setEditingItemId] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [editQuantity, setEditQuantity] = useState('')
  const [savingEdit, setSavingEdit] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  const fetchAll = useCallback(async () => {
    const [dayData, weekData] = await Promise.all([getForDay(), getForWeek()])
    return { dayData, weekData }
  }, [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const { dayData, weekData } = await fetchAll()
        if (!active) return
        setDay(dayData)
        setWeek(weekData)
      } catch (err) {
        if (active) {
          setError(
            getApiErrorMessage(err, {}, 'Não foi possível carregar sua nutrição.'),
          )
        }
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [fetchAll])

  async function handleRetry() {
    setLoading(true)
    setError(null)
    try {
      const { dayData, weekData } = await fetchAll()
      setDay(dayData)
      setWeek(weekData)
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível carregar sua nutrição.'),
      )
    } finally {
      setLoading(false)
    }
  }

  async function handleAdd(event: FormEvent<HTMLFormElement>, meal: MealType) {
    event.preventDefault()

    const trimmedName = itemName.trim()
    const trimmedQuantity = quantity.trim()

    if (!trimmedName) {
      setFormError('Informe o nome do item.')
      return
    }
    if (!trimmedQuantity) {
      setFormError('Informe a quantidade.')
      return
    }

    setSaving(true)
    setFormError(null)

    try {
      const created = await addItem(meal, trimmedName, trimmedQuantity)

      // Insere só no grupo da refeição. Diferente de finanças, aqui não há
      // total derivado — a estrutura do dia é a própria informação.
      setDay((current) =>
        current
          ? {
              ...current,
              meals: current.meals.map((group) =>
                group.meal === meal
                  ? { ...group, items: [...group.items, created] }
                  : group,
              ),
            }
          : current,
      )

      setItemName('')
      setQuantity('')
      // Formulário segue aberto: registrar uma refeição é listar vários itens
      // seguidos ("2 ovos", "1 pão", "café").
      itemInputRef.current?.focus()
    } catch (err) {
      setFormError(
        getApiErrorMessage(err, {}, 'Não foi possível adicionar o item.'),
      )
    } finally {
      setSaving(false)
    }
  }

  async function handleRemove(itemId: string, itemLabel: string, meal: MealType) {
    // Confirmação simples, mesmo padrão dos outros módulos.
    if (!window.confirm(`Remover "${itemLabel}"?`)) return

    setRemovingId(itemId)
    setError(null)

    try {
      await removeItem(itemId)
      setDay((current) =>
        current
          ? {
              ...current,
              meals: current.meals.map((group) =>
                group.meal === meal
                  ? {
                      ...group,
                      items: group.items.filter((item) => item.id !== itemId),
                    }
                  : group,
              ),
            }
          : current,
      )
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover o item.'))
    } finally {
      setRemovingId(null)
    }
  }

  function openForm(meal: MealType) {
    setOpenMeal(meal)
    setItemName('')
    setQuantity('')
    setFormError(null)
  }

  function startEdit(item: { id: string; itemName: string; quantity: string }) {
    setEditingItemId(item.id)
    setEditName(item.itemName)
    setEditQuantity(item.quantity)
    setEditError(null)
  }

  async function handleSaveEdit(
    event: FormEvent<HTMLFormElement>,
    itemId: string,
    meal: MealType,
  ) {
    event.preventDefault()

    const trimmedName = editName.trim()
    const trimmedQuantity = editQuantity.trim()

    if (!trimmedName) {
      setEditError('Informe o nome do item.')
      return
    }
    if (!trimmedQuantity) {
      setEditError('Informe a quantidade.')
      return
    }

    setSavingEdit(true)
    setEditError(null)

    try {
      const updated = await updateItem(itemId, trimmedName, trimmedQuantity)

      // Troca só o item no grupo da refeição. Como no add, não há total derivado
      // aqui — a resposta do PUT basta, sem segunda consulta.
      setDay((current) =>
        current
          ? {
              ...current,
              meals: current.meals.map((group) =>
                group.meal === meal
                  ? {
                      ...group,
                      items: group.items.map((item) =>
                        item.id === itemId ? updated : item,
                      ),
                    }
                  : group,
              ),
            }
          : current,
      )

      setEditingItemId(null)
    } catch (err) {
      setEditError(
        getApiErrorMessage(err, {}, 'Não foi possível salvar o item.'),
      )
    } finally {
      setSavingEdit(false)
    }
  }

  if (loading) return <LoadingState />

  if (error && !day) {
    return (
      <div className="flex flex-col items-center gap-4 py-12 text-center">
        <p className="text-sm text-red-300">{error}</p>
        <button
          type="button"
          onClick={handleRetry}
          className="rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95"
        >
          Tentar de novo
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-5">
      <header>
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Nutrição</h1>
      </header>

      <div role="tablist" className="flex gap-1 rounded-md bg-surface p-1">
        {(['hoje', 'semana', 'plano'] as const).map((option) => (
          <button
            key={option}
            type="button"
            role="tab"
            aria-selected={tab === option}
            onClick={() => setTab(option)}
            className={[
              'flex-1 rounded-lg px-3 py-2 text-sm font-medium transition',
              tab === option
                ? 'bg-surface-hi text-ink'
                : 'text-slate-400 hover:text-slate-200',
            ].join(' ')}
          >
            {option === 'hoje' ? 'Hoje' : option === 'semana' ? 'Semana' : 'Plano'}
          </button>
        ))}
      </div>

      {tab === 'hoje' ? (
        <div className="flex flex-col gap-3">
          {day?.meals.map((group) => (
            <section
              key={group.meal}
              className="rounded-md bg-surface px-4 py-3.5 ring-1 ring-line"
            >
              <div className="flex items-center justify-between">
                <h2 className="text-[11px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                  {MEAL_LABELS[group.meal]}
                </h2>
                <button
                  type="button"
                  onClick={() =>
                    openMeal === group.meal ? setOpenMeal(null) : openForm(group.meal)
                  }
                  aria-label={`Adicionar item em ${MEAL_LABELS[group.meal]}`}
                  className="rounded-lg p-1.5 text-slate-400 transition hover:bg-surface-hi hover:text-brand-green"
                >
                  <Plus size={18} />
                </button>
              </div>

              {/* Os itens já vivem dentro do card da refeição: divisórias
                  bastam para separá-los, sem cada um virar um bloco. */}
              {group.items.length > 0 ? (
                <ul className="mt-2 divide-y divide-line border-t border-line">
                  {group.items.map((item) =>
                    editingItemId === item.id ? (
                      <li key={item.id} className="py-2.5">
                        <form
                          onSubmit={(event) =>
                            handleSaveEdit(event, item.id, group.meal)
                          }
                          className="flex flex-col gap-2"
                        >
                          <div className="flex gap-2">
                            <input
                              type="text"
                              value={editName}
                              onChange={(event) => {
                                setEditName(event.target.value)
                                if (editError !== null) setEditError(null)
                              }}
                              autoFocus
                              maxLength={200}
                              aria-label="Nome do item"
                              className={inputClasses}
                            />
                            <input
                              type="text"
                              value={editQuantity}
                              onChange={(event) => {
                                setEditQuantity(event.target.value)
                                if (editError !== null) setEditError(null)
                              }}
                              maxLength={100}
                              aria-label="Quantidade"
                              className={`${inputClasses} max-w-32`}
                            />
                          </div>

                          <div className="flex gap-2">
                            <button
                              type="submit"
                              disabled={savingEdit}
                              className="flex-1 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                            >
                              {savingEdit ? 'Salvando...' : 'Salvar'}
                            </button>
                            <button
                              type="button"
                              onClick={() => setEditingItemId(null)}
                              className="rounded-md px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-surface hover:text-slate-200"
                            >
                              Cancelar
                            </button>
                          </div>

                          {editError && (
                            <p role="alert" className="text-sm text-red-300">
                              {editError}
                            </p>
                          )}
                        </form>
                      </li>
                    ) : (
                      <li
                        key={item.id}
                        className="flex items-center gap-2 py-2.5"
                      >
                        <span className="min-w-0 flex-1 truncate text-sm">
                          {item.itemName}
                        </span>
                        <span className="shrink-0 text-xs text-slate-400">
                          {item.quantity}
                        </span>
                        <ItemActions
                          busy={removingId === item.id}
                          onEdit={() => startEdit(item)}
                          onDelete={() =>
                            handleRemove(item.id, item.itemName, group.meal)
                          }
                          editLabel={`Editar ${item.itemName}`}
                          deleteLabel={`Remover ${item.itemName}`}
                        />
                      </li>
                    ),
                  )}
                </ul>
              ) : (
                openMeal !== group.meal && (
                  <p className="mt-1.5 text-xs text-slate-500">
                    Nada registrado.
                  </p>
                )
              )}

              {openMeal === group.meal && (
                <form
                  onSubmit={(event) => handleAdd(event, group.meal)}
                  className="mt-2 flex flex-col gap-2"
                >
                  <div className="flex gap-2">
                    <input
                      ref={itemInputRef}
                      type="text"
                      value={itemName}
                      onChange={(event) => {
                        setItemName(event.target.value)
                        if (formError !== null) setFormError(null)
                      }}
                      autoFocus
                      maxLength={200}
                      placeholder="Item (ex: ovos)"
                      aria-label="Nome do item"
                      className={inputClasses}
                    />
                    <input
                      type="text"
                      value={quantity}
                      onChange={(event) => {
                        setQuantity(event.target.value)
                        if (formError !== null) setFormError(null)
                      }}
                      maxLength={100}
                      placeholder="Qtd. (ex: 2)"
                      aria-label="Quantidade"
                      className={`${inputClasses} max-w-32`}
                    />
                  </div>

                  <div className="flex gap-2">
                    <button
                      type="submit"
                      disabled={saving}
                      className="flex-1 rounded-xl bg-brand-green px-3 py-2 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {saving ? 'Salvando...' : 'Adicionar'}
                    </button>
                    <button
                      type="button"
                      onClick={() => setOpenMeal(null)}
                      className="rounded-md px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-surface hover:text-slate-200"
                    >
                      Fechar
                    </button>
                  </div>

                  {formError && (
                    <p role="alert" className="text-sm text-red-300">
                      {formError}
                    </p>
                  )}
                </form>
              )}
            </section>
          ))}
        </div>
      ) : tab === 'plano' ? (
        <section className="flex flex-col gap-2">
          <SectionHeader>Plano da semana</SectionHeader>
          <NutritionPlanSection />
        </section>
      ) : (
        <section className="flex flex-col gap-2">
          <SectionHeader>Semana</SectionHeader>
          <p className="text-sm text-slate-500">
            Itens registrados por refeição em cada dia.
          </p>

          {week?.days.map((weekDay) => {
            const total = weekDay.meals.reduce(
              (sum, group) => sum + group.items.length,
              0,
            )
            return (
              <div
                key={weekDay.date}
                className="rounded-md bg-surface px-4 py-3 ring-1 ring-line"
              >
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium first-letter:uppercase">
                    {formatWeekdayShort(weekDay.date)}
                  </p>
                  <span className="text-xs text-slate-500 tabular-nums">
                    {total} {total === 1 ? 'item' : 'itens'}
                  </span>
                </div>

                {total > 0 ? (
                  <div className="mt-2 grid grid-cols-4 gap-1.5 text-center">
                    {MEAL_ORDER.map((meal) => {
                      const count =
                        weekDay.meals.find((group) => group.meal === meal)?.items
                          .length ?? 0
                      return (
                        <div
                          key={meal}
                          className={[
                            'rounded-lg px-1 py-1.5',
                            count > 0 ? 'bg-surface-hi' : 'bg-surface',
                          ].join(' ')}
                        >
                          <p className="text-[10px] text-slate-400">
                            {MEAL_SHORT_LABELS[meal]}
                          </p>
                          <p
                            className={[
                              'text-sm font-semibold tabular-nums',
                              count > 0 ? 'text-brand-green' : 'text-slate-600',
                            ].join(' ')}
                          >
                            {count}
                          </p>
                        </div>
                      )
                    })}
                  </div>
                ) : (
                  <p className="mt-1 text-xs text-slate-500">
                    Nenhum registro neste dia.
                  </p>
                )}
              </div>
            )
          })}
        </section>
      )}

      {error && day && (
        <p
          role="alert"
          className="rounded-lg bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}
    </div>
  )
}

export default Nutricao
