import { lazy, Suspense, useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  ArrowDownRight,
  ArrowUpRight,
  Eye,
  EyeOff,
  Plus,
  Trash2,
  TrendingDown,
  TrendingUp,
} from 'lucide-react'
import Segmented from '../../components/Segmented'
import SectionHeader from '../../components/SectionHeader'
import ItemActions from '../../components/ItemActions'
import WeekNav from '../../components/WeekNav'
import {
  createCategory,
  createEntry,
  deleteCategory,
  deleteEntry,
  getBalance,
  getBalanceHistory,
  getCategories,
  getWeeklySummary,
  updateEntry,
} from '../../services/financeService'
import { getApiErrorMessage } from '../../services/apiError'
import {
  addIsoDays,
  formatCurrency,
  formatShortDate,
  todayIso,
} from '../../utils/format'
import type {
  BalanceResponse,
  DailyBalanceResponse,
  FinanceCategoryResponse,
  FinanceEntryResponse,
  FinanceEntryType,
  WeeklyFinanceSummaryResponse,
} from '../../types/finance'

// Carregado sob demanda: o recharts sozinho dobra o bundle, e só esta tela usa
// gráfico. Assim ele vira um chunk separado, buscado ao abrir Finanças, e não
// pesa no primeiro carregamento do app.
const BalanceChart = lazy(() => import('../../components/BalanceChart'))

const BALANCE_HISTORY_DAYS = 30

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
  'w-full rounded-md bg-surface px-4 py-3 text-ink ring-1 ring-line transition outline-none placeholder:text-slate-500 focus:ring-2 focus:ring-brand-green'

const labelClasses = 'text-xs font-medium text-slate-400'

function LoadingState() {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-label="Carregando">
      <div className="h-24 animate-pulse rounded-md bg-surface" />
      <div className="h-20 animate-pulse rounded-md bg-surface" />
      <div className="h-16 animate-pulse rounded-md bg-surface" />
    </div>
  )
}

export function Financas() {
  const [balance, setBalance] = useState<BalanceResponse | null>(null)
  const [summary, setSummary] = useState<WeeklyFinanceSummaryResponse | null>(
    null,
  )
  const [categories, setCategories] = useState<FinanceCategoryResponse[]>([])
  const [history, setHistory] = useState<DailyBalanceResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  // Começa oculto: a tela pode ser aberta em público.
  const [balanceVisible, setBalanceVisible] = useState(false)

  // Navegação de semanas do resumo. viewedWeekStart null = semana atual (sem
  // ?inicio=). currentWeekStart, fixado na primeira carga, é o teto: não se
  // navega para o futuro. Só o resumo muda ao navegar — saldo e gráfico são
  // globais e continuam sempre "até hoje".
  const [viewedWeekStart, setViewedWeekStart] = useState<string | null>(null)
  const [currentWeekStart, setCurrentWeekStart] = useState<string | null>(null)
  const [weekNavBusy, setWeekNavBusy] = useState(false)

  // ?data= vem da Agenda: abre já com o formulário pronto naquela data.
  const [searchParams] = useSearchParams()
  const prefillDate = searchParams.get('data')

  // Formulário de lançamento.
  const [formOpen, setFormOpen] = useState(prefillDate !== null)
  const [amount, setAmount] = useState('')
  const [entryType, setEntryType] = useState<FinanceEntryType>('Saida')
  const [categoryId, setCategoryId] = useState('')
  const [date, setDate] = useState(prefillDate ?? todayIso())
  const [description, setDescription] = useState('')
  const [creating, setCreating] = useState(false)
  const [createError, setCreateError] = useState<string | null>(null)
  const amountInputRef = useRef<HTMLInputElement>(null)
  const entryFormRef = useRef<HTMLFormElement>(null)
  // Lançamento em edição: quando setado, o mesmo formulário do topo vira edição
  // (chama PUT, botão "Salvar") em vez de criação. Reusar o form evita duplicar
  // toda a estrutura de tipo/categoria/data/descrição numa versão inline.
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null)
  // Travas de linha em voo (remoção de lançamento / de categoria).
  const [deletingEntryId, setDeletingEntryId] = useState<string | null>(null)
  const [deletingCategoryId, setDeletingCategoryId] = useState<string | null>(
    null,
  )

  // Formulário de categoria.
  const [categoryFormOpen, setCategoryFormOpen] = useState(false)
  const [categoryName, setCategoryName] = useState('')
  const [creatingCategory, setCreatingCategory] = useState(false)
  const [categoryError, setCategoryError] = useState<string | null>(null)

  const fetchAll = useCallback(async () => {
    const [balanceData, summaryData, categoriesData, historyData] =
      await Promise.all([
        getBalance(),
        getWeeklySummary(),
        getCategories(),
        getBalanceHistory(BALANCE_HISTORY_DAYS),
      ])
    return { balanceData, summaryData, categoriesData, historyData }
  }, [])

  useEffect(() => {
    let active = true

    async function run() {
      try {
        const result = await fetchAll()
        if (!active) return
        setBalance(result.balanceData)
        setSummary(result.summaryData)
        // A primeira carga é sempre a semana atual (sem ?inicio=): fixa o teto.
        setCurrentWeekStart(result.summaryData.weekStart)
        setCategories(result.categoriesData)
        setHistory(result.historyData)
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
      setCurrentWeekStart(result.summaryData.weekStart)
      // Volta o resumo para a semana atual: o "tentar de novo" recarrega o estado
      // inicial, e fetchAll busca sempre a semana corrente.
      setViewedWeekStart(null)
      setCategories(result.categoriesData)
      setHistory(result.historyData)
      setCategoryId((current) => current || (result.categoriesData[0]?.id ?? ''))
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível carregar suas finanças.'),
      )
    } finally {
      setLoading(false)
    }
  }

  // Troca a semana do resumo. Saldo e gráfico não são refeitos — são globais e
  // não dependem da semana consultada.
  async function goToWeek(newWeekStart: string) {
    setWeekNavBusy(true)
    setError(null)

    try {
      const summaryData = await getWeeklySummary(newWeekStart)
      setSummary(summaryData)
      setViewedWeekStart(newWeekStart)
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível carregar a semana.'),
      )
    } finally {
      setWeekNavBusy(false)
    }
  }

  // Rebusca saldo, resumo e série após qualquer mutação de lançamento: os três
  // são derivados do conjunto de lançamentos e recalculá-los no cliente
  // duplicaria a soma do backend. Categorias ficam de fora — lançamento não as
  // altera.
  const refetchTotals = useCallback(async () => {
    // O resumo é o da semana VISÍVEL, não necessariamente a atual: editar um
    // lançamento enquanto se olha uma semana passada deve reconciliar aquela
    // semana, não pular de volta para a de hoje. Saldo e série seguem globais.
    const [balanceData, summaryData, historyData] = await Promise.all([
      getBalance(),
      getWeeklySummary(viewedWeekStart ?? undefined),
      getBalanceHistory(BALANCE_HISTORY_DAYS),
    ])
    setBalance(balanceData)
    setSummary(summaryData)
    // A série também muda: o lançamento desloca o saldo do seu dia em diante.
    setHistory(historyData)
  }, [viewedWeekStart])

  async function handleSubmitEntry(event: FormEvent<HTMLFormElement>) {
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

    const payload = {
      categoryId,
      amount: parsedAmount,
      type: entryType,
      date,
      description: description.trim() || null,
    }

    try {
      if (editingEntryId) {
        await updateEntry(editingEntryId, payload)
      } else {
        await createEntry(payload)
      }

      await refetchTotals()

      setAmount('')
      setDescription('')

      if (editingEntryId) {
        // Sai do modo edição: o formulário volta a ser de criação, sem fechar,
        // caso o usuário queira lançar algo em seguida.
        setEditingEntryId(null)
      }
      // Tipo, categoria e data permanecem: quem lança vários seguidos costuma
      // repetir o contexto.
      amountInputRef.current?.focus()
    } catch (err) {
      setCreateError(
        getApiErrorMessage(err, {}, 'Não foi possível salvar o lançamento.'),
      )
    } finally {
      setCreating(false)
    }
  }

  function startEditEntry(entry: FinanceEntryResponse) {
    setEditingEntryId(entry.id)
    // Number → string em pt-BR usa vírgula, coerente com o placeholder "0,00".
    setAmount(String(entry.amount).replace('.', ','))
    setEntryType(entry.type)
    setCategoryId(entry.categoryId)
    setDate(entry.date)
    setDescription(entry.description ?? '')
    setCreateError(null)
    setFormOpen(true)
    // Rola até o formulário: a lista fica bem abaixo dele, e sem isso a edição
    // pareceria não ter feito nada.
    requestAnimationFrame(() =>
      entryFormRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' }),
    )
  }

  function cancelEntryForm() {
    setFormOpen(false)
    setEditingEntryId(null)
    setAmount('')
    setDescription('')
    setCreateError(null)
  }

  async function handleDeleteEntry(entry: FinanceEntryResponse) {
    if (!window.confirm('Remover este lançamento?')) return

    setDeletingEntryId(entry.id)
    setError(null)

    try {
      await deleteEntry(entry.id)
      // Se removi o que estava em edição, saio do modo edição para o formulário
      // não continuar apontando para um lançamento que já não existe.
      if (editingEntryId === entry.id) cancelEntryForm()
      await refetchTotals()
    } catch (err) {
      setError(
        getApiErrorMessage(err, {}, 'Não foi possível remover o lançamento.'),
      )
    } finally {
      setDeletingEntryId(null)
    }
  }

  async function handleDeleteCategory(category: FinanceCategoryResponse) {
    if (!window.confirm(`Remover a categoria "${category.name}"?`)) return

    setDeletingCategoryId(category.id)
    setCategoryError(null)

    try {
      await deleteCategory(category.id)

      // Remoção bloqueia se houver lançamentos vinculados (409), então um sucesso
      // significa zero lançamentos afetados — saldo e totais não mudam, basta
      // tirar da lista local. Se era a selecionada, cai para a primeira restante.
      setCategories((current) => {
        const next = current.filter((c) => c.id !== category.id)
        setCategoryId((selected) =>
          selected === category.id ? (next[0]?.id ?? '') : selected,
        )
        return next
      })
    } catch (err) {
      setCategoryError(
        getApiErrorMessage(
          err,
          {
            409: 'Esta categoria tem lançamentos vinculados e não pode ser removida.',
          },
          'Não foi possível remover a categoria.',
        ),
      )
    } finally {
      setDeletingCategoryId(null)
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
        <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">Finanças</h1>
      </header>

      {/* SALDO ATUAL */}
      <section className="rounded-md bg-surface px-5 py-4 ring-1 ring-line">
        <div className="flex items-center justify-between">
          <SectionHeader>Saldo atual</SectionHeader>
          <button
            type="button"
            onClick={() => setBalanceVisible((visible) => !visible)}
            aria-pressed={balanceVisible}
            aria-label={balanceVisible ? 'Ocultar saldo' : 'Mostrar saldo'}
            className="rounded-lg p-1.5 text-slate-400 transition hover:bg-surface-hi hover:text-slate-200"
          >
            {balanceVisible ? <Eye size={16} /> : <EyeOff size={16} />}
          </button>
        </div>

        <p className="mt-1 text-3xl font-semibold text-ink tabular-nums glow-ink">
          {balanceVisible && balance
            ? formatCurrency(balance.currentBalance)
            : 'R$ ••••••'}
        </p>
      </section>

      {/* RESUMO DA SEMANA */}
      {summary && (
        <section className="rounded-md bg-surface px-5 py-4 ring-1 ring-line">
          <SectionHeader>Resumo da semana</SectionHeader>

          {/* Navegação de semanas: o intervalo visível fica entre as setas. A
              seta "próxima" trava na semana atual — resumo de semana futura não
              tem sentido. Só o resumo muda; saldo e gráfico seguem globais. */}
          <div className="mt-2">
            <WeekNav
              weekStart={summary.weekStart}
              weekEnd={summary.weekEnd}
              canGoNext={
                currentWeekStart !== null && summary.weekStart < currentWeekStart
              }
              busy={weekNavBusy}
              onPrev={() => goToWeek(addIsoDays(summary.weekStart, -7))}
              onNext={() => goToWeek(addIsoDays(summary.weekStart, 7))}
            />
          </div>

          {/* Saldo do período em destaque, entradas e saídas menores ao lado —
              mesma hierarquia do card do dashboard, com o ícone de tendência
              colado ao valor. */}
          <p
            className={[
              'mt-2 text-3xl font-semibold tabular-nums',
              summary.periodBalance < 0 ? 'text-red-400' : 'glow-ink text-ink',
            ].join(' ')}
          >
            {formatCurrency(summary.periodBalance)}
          </p>

          <div className="mt-2 flex items-center gap-4 text-sm">
            <span className="flex items-center gap-1">
              <TrendingUp
                size={15}
                className="shrink-0 text-brand-green"
                aria-hidden="true"
              />
              <span className="sr-only">Entradas:</span>
              <span className="text-slate-300 tabular-nums">
                {formatCurrency(summary.periodTotalIn)}
              </span>
            </span>

            <span className="flex items-center gap-1">
              <TrendingDown
                size={15}
                className="shrink-0 text-red-400"
                aria-hidden="true"
              />
              <span className="sr-only">Saídas:</span>
              <span className="text-slate-300 tabular-nums">
                {formatCurrency(summary.periodTotalOut)}
              </span>
            </span>
          </div>
        </section>
      )}

      <Suspense
        fallback={
          <div className="h-64 animate-pulse rounded-md bg-surface" aria-hidden="true" />
        }
      >
        <BalanceChart history={history} days={BALANCE_HISTORY_DAYS} />
      </Suspense>

      {/* FORMULÁRIO DE LANÇAMENTO */}
      {formOpen ? (
        <form
          ref={entryFormRef}
          onSubmit={handleSubmitEntry}
          className="flex flex-col gap-3 rounded-md bg-surface p-3 ring-1 ring-line"
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
              {creating
                ? 'Salvando...'
                : editingEntryId
                  ? 'Salvar'
                  : 'Adicionar lançamento'}
            </button>
            <button
              type="button"
              onClick={cancelEntryForm}
              className="rounded-md px-4 py-2.5 font-medium text-slate-400 transition hover:bg-surface hover:text-slate-200"
            >
              {editingEntryId ? 'Cancelar' : 'Fechar'}
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
          className="flex min-h-12 w-full items-center justify-center gap-2 rounded-md border-2 border-dashed border-line text-sm font-medium text-slate-400 transition hover:border-slate-600 hover:bg-surface hover:text-slate-200"
        >
          <Plus size={18} aria-hidden="true" />
          Novo lançamento
        </button>
      )}

      {/* NOVA CATEGORIA — formulário curto, aninhado ao contexto do lançamento. */}
      {categoryFormOpen && (
        <form
          onSubmit={handleCreateCategory}
          className="flex flex-col gap-2 rounded-md bg-surface p-3 ring-1 ring-line"
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

          {/* Só as categorias próprias podem ser removidas; as padrão do sistema
              nem aparecem aqui. A remoção é bloqueada pelo backend (409) se
              houver lançamentos vinculados. */}
          {categories.some((category) => !category.isDefault) && (
            <div className="mt-1 flex flex-col gap-1 border-t border-line pt-2">
              <p className={labelClasses}>Suas categorias</p>
              <ul className="flex flex-col divide-y divide-line">
                {categories
                  .filter((category) => !category.isDefault)
                  .map((category) => (
                    <li
                      key={category.id}
                      className="flex items-center gap-2 py-1.5"
                    >
                      <span className="min-w-0 flex-1 truncate text-sm">
                        {category.name}
                      </span>
                      <button
                        type="button"
                        disabled={deletingCategoryId === category.id}
                        onClick={() => handleDeleteCategory(category)}
                        aria-label={`Remover categoria ${category.name}`}
                        className="shrink-0 rounded p-1.5 text-slate-500 transition hover:bg-surface-hi hover:text-red-400 disabled:opacity-50"
                      >
                        <Trash2 size={15} />
                      </button>
                    </li>
                  ))}
              </ul>
            </div>
          )}
        </form>
      )}

      {/* LANÇAMENTOS DA SEMANA */}
      <section className="flex flex-col gap-2">
        <SectionHeader>Lançamentos da semana</SectionHeader>

        {summary && summary.entries.length > 0 ? (
          <ul className="divide-y divide-line overflow-hidden rounded-md bg-surface ring-1 ring-line">
            {summary.entries.map((entry) => {
              const isIncome = entry.type === 'Entrada'
              const Arrow = isIncome ? ArrowUpRight : ArrowDownRight
              return (
                <li
                  key={entry.id}
                  className="flex items-center gap-3 px-4 py-3.5"
                >
                  {/* O disco é neutro nos dois casos; quem carrega o sinal é a
                      seta e o valor. Antes o fundo era tingido de verde/vermelho
                      e criava manchas de cor ao longo da lista inteira. */}
                  <span
                    aria-hidden="true"
                    className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-hi"
                  >
                    <Arrow size={16} className={ENTRY_TYPE_COLORS[entry.type]} />
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

                  <ItemActions
                    busy={deletingEntryId === entry.id}
                    onEdit={() => startEditEntry(entry)}
                    onDelete={() => handleDeleteEntry(entry)}
                    editLabel="Editar lançamento"
                    deleteLabel="Remover lançamento"
                  />
                </li>
              )
            })}
          </ul>
        ) : (
          <div className="rounded-md bg-surface px-5 py-8 text-center ring-1 ring-line">
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
