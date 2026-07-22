import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Plus, X } from 'lucide-react'
import {
  addItem,
  getForDay,
  getForWeek,
  removeItem,
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
  'w-full rounded-xl bg-white/5 px-3 py-2.5 text-sm text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-10 animate-pulse rounded-xl bg-white/5" />
      <div className="h-28 animate-pulse rounded-2xl bg-white/5" />
      <div className="h-28 animate-pulse rounded-2xl bg-white/5" />
    </div>
  )
}

type Tab = 'hoje' | 'semana'

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

  async function handleRemove(itemId: string, meal: MealType) {
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
        <h1 className="font-display text-3xl tracking-tight">Nutrição</h1>
      </header>

      <div role="tablist" className="flex gap-1 rounded-xl bg-white/5 p-1">
        {(['hoje', 'semana'] as const).map((option) => (
          <button
            key={option}
            type="button"
            role="tab"
            aria-selected={tab === option}
            onClick={() => setTab(option)}
            className={[
              'flex-1 rounded-lg px-3 py-2 text-sm font-medium transition',
              tab === option
                ? 'bg-white/10 text-slate-100'
                : 'text-slate-400 hover:text-slate-200',
            ].join(' ')}
          >
            {option === 'hoje' ? 'Hoje' : 'Semana'}
          </button>
        ))}
      </div>

      {tab === 'hoje' ? (
        <div className="flex flex-col gap-3">
          {day?.meals.map((group) => (
            <section
              key={group.meal}
              className="rounded-2xl bg-white/5 px-4 py-3.5 ring-1 ring-white/10"
            >
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-medium text-slate-200">
                  {MEAL_LABELS[group.meal]}
                </h2>
                <button
                  type="button"
                  onClick={() =>
                    openMeal === group.meal ? setOpenMeal(null) : openForm(group.meal)
                  }
                  aria-label={`Adicionar item em ${MEAL_LABELS[group.meal]}`}
                  className="rounded-lg p-1.5 text-slate-400 transition hover:bg-white/10 hover:text-brand-green"
                >
                  <Plus size={18} />
                </button>
              </div>

              {group.items.length > 0 ? (
                <ul className="mt-2 flex flex-col gap-1.5">
                  {group.items.map((item) => (
                    <li
                      key={item.id}
                      className="flex items-center gap-2 rounded-lg bg-white/5 px-3 py-2"
                    >
                      <span className="min-w-0 flex-1 truncate text-sm">
                        {item.itemName}
                      </span>
                      <span className="shrink-0 text-xs text-slate-400">
                        {item.quantity}
                      </span>
                      <button
                        type="button"
                        disabled={removingId === item.id}
                        onClick={() => handleRemove(item.id, group.meal)}
                        aria-label={`Remover ${item.itemName}`}
                        className="shrink-0 rounded p-1 text-slate-500 transition hover:bg-white/10 hover:text-red-400 disabled:opacity-50"
                      >
                        <X size={14} />
                      </button>
                    </li>
                  ))}
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
                      className="rounded-xl px-3 py-2 text-sm font-medium text-slate-400 transition hover:bg-white/5 hover:text-slate-200"
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
      ) : (
        <section className="flex flex-col gap-2">
          <p className="text-sm text-slate-400">
            Itens registrados por refeição em cada dia da semana.
          </p>

          {week?.days.map((weekDay) => {
            const total = weekDay.meals.reduce(
              (sum, group) => sum + group.items.length,
              0,
            )
            return (
              <div
                key={weekDay.date}
                className="rounded-xl bg-white/5 px-4 py-3 ring-1 ring-white/10"
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
                            count > 0 ? 'bg-brand-green/10' : 'bg-white/5',
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
