import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ArrowDownRight, ArrowUpRight, Eye, EyeOff, Plus } from 'lucide-react'
import Segmented from '../../components/Segmented'
import {
  createCategory,
  createEntry,
  getBalance,
  getCategories,
  getWeeklySummary,
} from '../../services/financeService'
import { getApiErrorMessage } from '../../services/apiError'
import { formatCurrency, formatShortDate, todayIso } from '../../utils/format'
import type {
  BalanceResponse,
  FinanceCategoryResponse,
  FinanceEntryResponse,
  FinanceEntryType,
  WeeklyFinanceSummaryResponse,
} from '../../types/finance'

const ENTRY_TYPES: readonly FinanceEntryType[] = ['Entrada', 'Saida']

const ENTRY_TYPE_LABELS: Record<FinanceEntryType, string> = {
  Entrada: 'Entrada',
  Saida: 'Saída',
}

const ENTRY_TYPE_COLORS: Record<FinanceEntryType, string> = {
  Entrada: 'text-brand-green',
  Saida: 'text-red-400',
}

const inputClasses =
  'w-full rounded-xl bg-white/5 px-4 py-3 text-slate-100 ring-1 ring-white/10 transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-xs font-medium text-slate-400'

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-24 animate-pulse rounded-2xl bg-white/5" />
      <div className="h-20 animate-pulse rounded-2xl bg-white/5" />
      <div className="h-16 animate-pulse rounded-xl bg-white/5" />
    </div>
  )
}

export function Financas() {
  const [balance, setBalance] = useState<BalanceResponse | null>(null)
  const [summary, setSummary] = useState<WeeklyFinanceSummaryResponse | null>(
    null,
  )
  const [categories, setCategories] = useState<FinanceCategoryResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  // Começa oculto: a tela pode ser aberta em público.
  const [balanceVisible, setBalanceVisible] = useState(false)

  // Formulário de lançamento.
  const [formOpen, setFormOpen] = useState(false)
  const [amount, setAmount] = useState('')
  const [entryType, setEntryType] = useState<FinanceEntryType>('Saida')
  const [categoryId, setCategoryId] = useState('')
  const [date, setDate] = useState(todayIso())
  const [description, setDescription] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const amountInputRef = useRef<HTMLInputElement>(null)

  // Formulário de categoria.
  const [categoryFormOpen, setCategoryFormOpen] = useState(false)
  const [categoryName, setCategoryName] = useState('')
  const [creatingCategory, setCreatingCategory] = useState(false)
  const [categoryError, setCategoryError] = useState<string | null>(null)

  const fetchAll = useCallback(async () => {
    const [balanceData, summaryData, categoriesData] = await Promise.all([
      getBalance(),
      getWeeklySummary(),
      getCategories(),
    ])
    return { balanceData, summaryData, categoriesData }
  }, [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const result = await fetchAll()
        if (!active) return
        setBalance(result.balanceData)
        setSummary(result.summaryData)
        setCategories(result.categoriesData)
        // Pré-seleciona a primeira categoria para o formulário nunca abrir sem
        // seleção — o backend exige categoryId.
        setCategoryId((current) => current || (result.categoriesData[0]?.id ?? ''))
      } catch (err) {
        if (active) {
          setError(
            getApiErrorMessage(err, {}, 'Não foi possível carregar suas finanças.'),
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
      const result = await fetchAll()
      setBalance(result.balanceData)
      setSummary(result.summaryData)
      setCategories(result.categoriesData)
      setCategoryId((current) => current || (result.categoriesData[0]?.id ?? ''))
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível carregar suas finanças.'),
      )
    } finally {
      setLoading(false)
    }
  }

  async function handleCreateEntry(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCreateError(null)

    // Vírgula é o separador natural em pt-BR, mas Number() só entende ponto.
    const parsedAmount = Number(amount.replace(',', '.'))
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setCreateError('Informe um valor maior que zero.')
      return
    }
    if (!categoryId) {
      setCreateError('Escolha uma categoria.')
      return
    }

    setCreating(true)

    try {
      await createEntry({
        categoryId,
        amount: parsedAmount,
        type: entryType,
        date,
        description: description.trim() || null,
      })

      // Rebusca em vez de inserir na lista: o lançamento muda saldo e totais da
      // semana, e recalcular isso no cliente duplicaria a soma do backend. Só
      // não recarrego as categorias, que o lançamento não altera.
      const [balanceData, summaryData] = await Promise.all([
        getBalance(),
        getWeeklySummary(),
      ])
      setBalance(balanceData)
      setSummary(summaryData)

      setAmount('')
      setDescription('')
      // Tipo, categoria e data permanecem: quem lança vários seguidos costuma
      // repetir o contexto.
      amountInputRef.current?.focus()
    } catch (err) {
      setCreateError(
        getApiErrorMessage(err, {}, 'Não foi possível registrar o lançamento.'),
      )
    } finally {
      setCreating(false)
    }
  }

  async function handleCreateCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const name = categoryName.trim()
    if (!name) return

    setCreatingCategory(true)
    setCategoryError(null)

    try {
      const created = await createCategory(name)
      setCategories((current) =>
        [...current, created].sort((a, b) => {
          // Padrão do sistema primeiro, depois alfabética — mesma ordem do backend.
          if (a.isDefault !== b.isDefault) return a.isDefault ? -1 : 1
          return a.name.localeCompare(b.name, 'pt-BR')
        }),
      )
      // Já seleciona a recém-criada: quem acabou de criar quer usá-la agora.
      setCategoryId(created.id)
      setCategoryName('')
      setCategoryFormOpen(false)
    } catch (err) {
      setCategoryError(
        getApiErrorMessage(
          err,
          { 409: 'Você já tem uma categoria com esse nome.' },
          'Não foi possível criar a categoria.',
        ),
      )
    } finally {
      setCreatingCategory(false)
    }
  }

  // O lançamento carrega só o categoryId; o nome vem da lista já carregada.
  function findCategoryName(entry: FinanceEntryResponse): string {
    return (
      categories.find((category) => category.id === entry.categoryId)?.name ??
      'Sem categoria'
    )
  }

  if (loading) return <LoadingState />

  if (error && !balance) {
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
        <h1 className="font-display text-3xl tracking-tight">Finanças</h1>
      </header>

      {/* SALDO ATUAL */}
      <section className="rounded-2xl bg-white/5 px-5 py-4 ring-1 ring-white/10">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-medium text-slate-300">Saldo atual</h2>
          <button
            type="button"
            onClick={() => setBalanceVisible((visible) => !visible)}
            aria-pressed={balanceVisible}
            aria-label={balanceVisible ? 'Ocultar saldo' : 'Mostrar saldo'}
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-white/10 hover:text-slate-200"
          >
            {balanceVisible ? <Eye size={16} /> : <EyeOff size={16} />}
          </button>
        </div>

        <p className="mt-1 text-3xl font-semibold tabular-nums">
          {balanceVisible && balance
            ? formatCurrency(balance.currentBalance)
            : 'R$ ••••••'}
        </p>
      </section>

      {/* RESUMO DA SEMANA */}
      {summary && (
        <section className="rounded-2xl bg-white/5 px-5 py-4 ring-1 ring-white/10">
          <h2 className="text-sm font-medium text-slate-300">
            Semana de {formatShortDate(summary.weekStart)} a{' '}
            {formatShortDate(summary.weekEnd)}
          </h2>

          <div className="mt-3 grid grid-cols-3 gap-2 text-center">
            <div>
              <p className="text-xs text-slate-400">Entradas</p>
              <p className="mt-0.5 font-semibold text-brand-green tabular-nums">
                {formatCurrency(summary.periodTotalIn)}
              </p>
            </div>
            <div>
              <p className="text-xs text-slate-400">Saídas</p>
              <p className="mt-0.5 font-semibold text-red-400 tabular-nums">
                {formatCurrency(summary.periodTotalOut)}
              </p>
            </div>
            <div>
              <p className="text-xs text-slate-400">Do período</p>
              <p
                className={[
                  'mt-0.5 font-semibold tabular-nums',
                  summary.periodBalance < 0 ? 'text-red-400' : 'text-slate-100',
                ].join(' ')}
              >
                {formatCurrency(summary.periodBalance)}
              </p>
            </div>
          </div>
        </section>
      )}

      {/* FORMULÁRIO DE LANÇAMENTO */}
      {formOpen ? (
        <form
          onSubmit={handleCreateEntry}
          className="flex flex-col gap-3 rounded-xl bg-white/5 p-3 ring-1 ring-white/10"
        >
          <Segmented
            label="Tipo de lançamento"
            options={ENTRY_TYPES}
            value={entryType}
            onChange={setEntryType}
            labels={ENTRY_TYPE_LABELS}
            activeColors={ENTRY_TYPE_COLORS}
          />

          <div className="flex flex-col gap-1">
            <label htmlFor="valor" className={labelClasses}>
              Valor (R$)
            </label>
            <input
              id="valor"
              ref={amountInputRef}
              type="text"
              inputMode="decimal"
              value={amount}
              onChange={(event) => {
                setAmount(event.target.value)
                if (createError !== null) setCreateError(null)
              }}
              autoFocus
              placeholder="0,00"
              className={inputClasses}
            />
          </div>

          <div className="flex flex-col gap-1">
            <div className="flex items-center justify-between">
              <label htmlFor="categoria" className={labelClasses}>
                Categoria
              </label>
              <button
                type="button"
                onClick={() => setCategoryFormOpen((open) => !open)}
                className="text-xs font-medium text-brand-green transition hover:brightness-110"
              >
                {categoryFormOpen ? 'Cancelar' : '+ Nova categoria'}
              </button>
            </div>
            <select
              id="categoria"
              value={categoryId}
              onChange={(event) => setCategoryId(event.target.value)}
              className={inputClasses}
            >
              {categories.map((category) => (
                <option
                  key={category.id}
                  value={category.id}
                  className="bg-brand-dark"
                >
                  {category.name}
                </option>
              ))}
            </select>
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="data-lancamento" className={labelClasses}>
              Data
            </label>
            <input
              id="data-lancamento"
              type="date"
              value={date}
              onChange={(event) => setDate(event.target.value)}
              className={inputClasses}
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="descricao" className={labelClasses}>
              Descrição (opcional)
            </label>
            <input
              id="descricao"
              type="text"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              maxLength={500}
              placeholder="Ex: almoço de sexta"
              className={inputClasses}
            />
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={creating}
              className="flex-1 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {creating ? 'Salvando...' : 'Adicionar lançamento'}
            </button>
            <button
              type="button"
              onClick={() => {
                setFormOpen(false)
                setAmount('')
                setDescription('')
                setCreateError(null)
              }}
              className="rounded-xl px-4 py-2.5 font-medium text-slate-400 transition hover:bg-white/5 hover:text-slate-200"
            >
              Fechar
            </button>
          </div>

          {createError && (
            <p role="alert" className="text-sm text-red-300">
              {createError}
            </p>
          )}
        </form>
      ) : (
        <button
          type="button"
          onClick={() => setFormOpen(true)}
          className="flex min-h-12 w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-white/15 text-sm font-medium text-slate-400 transition hover:border-white/25 hover:bg-white/5 hover:text-slate-200"
        >
          <Plus size={18} aria-hidden="true" />
          Novo lançamento
        </button>
      )}

      {/* NOVA CATEGORIA — formulário curto, aninhado ao contexto do lançamento. */}
      {categoryFormOpen && (
        <form
          onSubmit={handleCreateCategory}
          className="flex flex-col gap-2 rounded-xl bg-white/5 p-3 ring-1 ring-white/10"
        >
          <label htmlFor="nova-categoria" className={labelClasses}>
            Nome da nova categoria
          </label>
          <div className="flex gap-2">
            <input
              id="nova-categoria"
              type="text"
              value={categoryName}
              onChange={(event) => {
                setCategoryName(event.target.value)
                if (categoryError !== null) setCategoryError(null)
              }}
              maxLength={100}
              placeholder="Ex: Pets"
              className={inputClasses}
            />
            <button
              type="submit"
              disabled={creatingCategory || categoryName.trim().length === 0}
              className="shrink-0 rounded-xl bg-brand-green px-4 py-2.5 font-semibold text-brand-dark transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {creatingCategory ? '...' : 'Criar'}
            </button>
          </div>
          {categoryError && (
            <p role="alert" className="text-sm text-red-300">
              {categoryError}
            </p>
          )}
        </form>
      )}

      {/* LANÇAMENTOS DA SEMANA */}
      <section className="flex flex-col gap-2">
        <h2 className="text-sm font-medium text-slate-300">
          Lançamentos da semana
        </h2>

        {summary && summary.entries.length > 0 ? (
          <ul className="flex flex-col gap-2">
            {summary.entries.map((entry) => {
              const isIncome = entry.type === 'Entrada'
              const Arrow = isIncome ? ArrowUpRight : ArrowDownRight
              return (
                <li
                  key={entry.id}
                  className="flex items-center gap-3 rounded-xl bg-white/5 px-4 py-3.5 ring-1 ring-white/10"
                >
                  <span
                    aria-hidden="true"
                    className={[
                      'flex size-8 shrink-0 items-center justify-center rounded-full',
                      isIncome ? 'bg-brand-green/10' : 'bg-red-400/10',
                    ].join(' ')}
                  >
                    <Arrow
                      size={16}
                      className={ENTRY_TYPE_COLORS[entry.type]}
                    />
                  </span>

                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">
                      {findCategoryName(entry)}
                    </p>
                    <p className="mt-0.5 truncate text-xs text-slate-400">
                      {entry.description ?? formatShortDate(entry.date)}
                    </p>
                  </div>

                  <span
                    className={[
                      'shrink-0 font-semibold tabular-nums',
                      ENTRY_TYPE_COLORS[entry.type],
                    ].join(' ')}
                  >
                    {isIncome ? '+' : '−'}
                    {formatCurrency(entry.amount)}
                  </span>
                </li>
              )
            })}
          </ul>
        ) : (
          <div className="rounded-2xl bg-white/5 px-5 py-8 text-center ring-1 ring-white/10">
            <p className="font-medium text-slate-200">
              Nenhum lançamento nesta semana.
            </p>
            <p className="mt-1.5 text-sm text-slate-400">
              Registre entradas e saídas para acompanhar para onde seu dinheiro
              está indo.
            </p>
          </div>
        )}
      </section>

      {error && balance && (
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

export default Financas
