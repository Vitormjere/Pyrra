// Helpers compartilhados por várias telas. Ficam aqui porque a conversão de data
// tem uma armadilha que não pode ser reimplementada errado em cada página.

/**
 * Data local no formato "YYYY-MM-DD", o mesmo do DateOnly do backend.
 *
 * NÃO use toISOString(): ele converte para UTC e, a partir das 21h no horário de
 * Brasília, devolveria o dia seguinte.
 */
export function toIsoDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

/** Hoje, no fuso do dispositivo, como "YYYY-MM-DD". */
export function todayIso(): string {
  return toIsoDate(new Date())
}

/**
 * Converte "YYYY-MM-DD" em Date sem passar pelo parser de string do JS, que
 * interpretaria a data como UTC e exibiria o dia anterior em fusos negativos.
 */
function parseIsoDate(isoDate: string): Date {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day)
}

/** "22/07" */
export function formatShortDate(isoDate: string): string {
  return new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: '2-digit',
  }).format(parseIsoDate(isoDate))
}

/** "quarta-feira, 22 de julho" */
export function formatDayLabel(isoDate: string): string {
  return new Intl.DateTimeFormat('pt-BR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(parseIsoDate(isoDate))
}

/** "qua, 22/07" — compacto, para listas de vários dias. */
export function formatWeekdayShort(isoDate: string): string {
  return new Intl.DateTimeFormat('pt-BR', {
    weekday: 'short',
    day: '2-digit',
    month: '2-digit',
  }).format(parseIsoDate(isoDate))
}

const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value)
}

// Sem casas fixas: 5 vira "5" e não "5,00"; 7,5 continua "7,5".
const numberFormatter = new Intl.NumberFormat('pt-BR', {
  maximumFractionDigits: 2,
})

export function formatNumber(value: number): string {
  return numberFormatter.format(value)
}

/**
 * Rótulo de um exercício planejado: "Supino reto — 4x10", ou só o nome quando
 * não há séries/repetições (caso de Corrida, ou de Academia sem detalhe).
 * Compartilhado entre a tela Treino e o dashboard, para os dois exibirem a
 * mesma coisa.
 */
export function formatPlannedExercise(
  exerciseName: string,
  sets: number | null,
  reps: number | null,
): string {
  if (sets === null || reps === null) return exerciseName
  return `${exerciseName} — ${sets}x${reps}`
}
