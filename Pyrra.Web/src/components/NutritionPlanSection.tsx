import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Plus, X } from 'lucide-react'
import {
  addPlanItem,
  getPlan,
  removePlanItem,
} from '../services/nutritionService'
import { getApiErrorMessage } from '../services/apiError'
import { WEEK_DAY_LABELS, todayWeekDay } from '../types/plan'
import type { WeekDay } from '../types/plan'
import type { MealType, PlanDayResponse } from '../types/nutrition'

const MEAL_LABELS: Record<MealType, string> = {
  CafeDaManha: 'Café da manhã',
  Almoco: 'Almoço',
  Lanche: 'Lanche',
  Jantar: 'Jantar',
}

const inputClasses =
  'w-full rounded-md bg-surface-hi px-3 py-2 text-sm text-ink ring-1 ring-line transition outline-none placeholder:text-slate-600 focus:ring-2 focus:ring-brand-green'

// Chave do formulário aberto: dia + refeição. Só um por vez, senão a grade
// viraria 28 formulários empilhados.
type OpenSlot = { day: WeekDay; meal: MealType } | null

export function NutritionPlanSection() {
  const [plan, setPlan] = useState<PlanDayResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  // Dia expandido. Começa no de hoje: é o mais provável de se querer ajustar.
  const [openDay, setOpenDay] = useState<WeekDay>(todayWeekDay())
  const [openSlot, setOpenSlot] = useState<OpenSlot>(null)
  const [itemName, setItemName] = useState('')
  const [quantity, setQuantity] = useState('')
  const [saving, setSaving] = useState(false)
  const [removingId, setRemovingId] = useState<string | null>(null)
  const itemInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const data = await getPlan()
        if (active) setPlan(data)
      } catch (err) {
        if (active) {
          setError(getApiErrorMessage(err, {}, 'Não foi possível carregar o plano.'))
        }
      } finally {
        if (active) setLoading(false)
      }
    }

    void run()
    return () => {
      active = false
    }
  }, [])

  async function handleAdd(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!openSlot) return

    const name = itemName.trim()
    const qty = quantity.trim()
    if (!name || !qty) {
      setError('Informe o item e a quantidade.')
      return
    }

    setSaving(true)
    setError(null)

    try {
      const created = await addPlanItem(openSlot.day, openSlot.meal, name, qty)

      setPlan((current) =>
        current.map((day) =>
          day.day === openSlot.day
            ? {
                ...day,
                meals: day.meals.map((group) =>
                  group.meal === openSlot.meal
                    ? { ...group, items: [...group.items, created] }
                    : group,
                ),
              }
            : day,
        ),
      )

      setItemName('')
      setQuantity('')
      // Formulário segue aberto: montar uma refeição é listar vários itens.
      itemInputRef.current?.focus()
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível adicionar o item.'))
    } finally {
      setSaving(false)
    }
  }

  async function handleRemove(day: WeekDay, meal: MealType, itemId: string) {
    setRemovingId(itemId)
    setError(null)

    try {
      await removePlanItem(itemId)
      setPlan((current) =>
        current.map((planDay) =>
          planDay.day === day
            ? {
                ...planDay,
                meals: planDay.meals.map((group) =>
                  group.meal === meal
                    ? {
                        ...group,
                        items: group.items.filter((item) => item.id !== itemId),
                      }
                    : group,
                ),
              }
            : planDay,
        ),
      )
    } catch (err) {
      setError(getApiErrorMessage(err, {}, 'Não foi possível remover o item.'))
    } finally {
      setRemovingId(null)
    }
  }

  if (loading) {
    return (
      <div className="flex flex-col gap-2" aria-busy="true" aria-label="Carregando">
        <div className="h-12 animate-pulse rounded-md bg-surface" />
        <div className="h-12 animate-pulse rounded-md bg-surface" />
      </div>
    )
  }

  const today = todayWeekDay()

  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm text-slate-500">
        O que estiver planejado para um dia é copiado automaticamente para os
        registros quando aquele dia chega.
      </p>

      {/* Acordeão por dia: sete grades abertas ao mesmo tempo seria uma tela
          longa demais para o celular. */}
      {plan.map((planDay) => {
        const expanded = planDay.day === openDay
        const total = planDay.meals.reduce(
          (sum, group) => sum + group.items.length,
          0,
        )

        return (
          <div
            key={planDay.day}
            className="overflow-hidden rounded-md bg-surface ring-1 ring-line"
          >
            <button
              type="button"
              onClick={() => setOpenDay(expanded ? ('' as WeekDay) : planDay.day)}
              aria-expanded={expanded}
              className="flex w-full items-center justify-between px-4 py-3 text-left transition hover:bg-surface-hi"
            >
              <span
                className={[
                  'text-sm font-medium',
                  planDay.day === today ? 'text-brand-green' : 'text-ink',
                ].join(' ')}
              >
                {WEEK_DAY_LABELS[planDay.day]}
              </span>
              <span className="text-xs text-slate-500 tabular-nums">
                {total} {total === 1 ? 'item' : 'itens'}
              </span>
            </button>

            {expanded && (
              <div className="divide-y divide-line border-t border-line">
                {planDay.meals.map((group) => {
                  const slotOpen =
                    openSlot?.day === planDay.day && openSlot?.meal === group.meal

                  return (
                    <div key={group.meal} className="px-4 py-3">
                      <div className="flex items-center justify-between">
                        <span className="text-[11px] font-semibold tracking-[0.14em] text-slate-500 uppercase">
                          {MEAL_LABELS[group.meal]}
                        </span>
                        <button
                          type="button"
                          onClick={() => {
                            setOpenSlot(
                              slotOpen
                                ? null
                                : { day: planDay.day, meal: group.meal },
                            )
                            setItemName('')
                            setQuantity('')
                            setError(null)
                          }}
                          aria-label={`Adicionar item em ${MEAL_LABELS[group.meal]}`}
                          className="rounded p-1 text-slate-500 transition hover:bg-surface-hi hover:text-brand-green"
                        >
                          <Plus size={16} />
                        </button>
                      </div>

                      {group.items.length > 0 ? (
                        <ul className="mt-1.5 flex flex-col gap-1">
                          {group.items.map((item) => (
                            <li
                              key={item.id}
                              className="flex items-center gap-2 text-sm"
                            >
                              <span className="min-w-0 flex-1 truncate text-ink">
                                {item.itemName}
                              </span>
                              <span className="shrink-0 text-xs text-slate-400">
                                {item.quantity}
                              </span>
                              <button
                                type="button"
                                disabled={removingId === item.id}
                                onClick={() =>
                                  handleRemove(planDay.day, group.meal, item.id)
                                }
                                aria-label={`Remover ${item.itemName}`}
                                className="shrink-0 rounded p-1 text-slate-600 transition hover:text-red-400 disabled:opacity-50"
                              >
                                <X size={13} />
                              </button>
                            </li>
                          ))}
                        </ul>
                      ) : (
                        !slotOpen && (
                          <p className="mt-1 text-xs text-slate-600">
                            Nada planejado.
                          </p>
                        )
                      )}

                      {slotOpen && (
                        <form
                          onSubmit={handleAdd}
                          className="mt-2 flex flex-col gap-2"
                        >
                          <div className="flex gap-2">
                            <input
                              ref={itemInputRef}
                              type="text"
                              value={itemName}
                              onChange={(event) => setItemName(event.target.value)}
                              autoFocus
                              maxLength={200}
                              placeholder="Item"
                              aria-label="Nome do item"
                              className={inputClasses}
                            />
                            <input
                              type="text"
                              value={quantity}
                              onChange={(event) => setQuantity(event.target.value)}
                              maxLength={100}
                              placeholder="Qtd."
                              aria-label="Quantidade"
                              className={`${inputClasses} max-w-28`}
                            />
                          </div>
                          <div className="flex gap-2">
                            <button
                              type="submit"
                              disabled={saving}
                              className="flex-1 rounded-xl bg-brand-green px-3 py-1.5 text-sm font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
                            >
                              {saving ? 'Salvando...' : 'Adicionar'}
                            </button>
                            <button
                              type="button"
                              onClick={() => setOpenSlot(null)}
                              className="rounded-md px-3 py-1.5 text-sm text-slate-400 transition hover:text-slate-200"
                            >
                              Fechar
                            </button>
                          </div>
                        </form>
                      )}
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        )
      })}

      {error && (
        <p
          role="alert"
          className="rounded-md bg-red-500/10 px-3 py-2 text-center text-sm text-red-300 ring-1 ring-red-500/20"
        >
          {error}
        </p>
      )}
    </div>
  )
}

export default NutritionPlanSection
