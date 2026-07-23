// Espelha o enum WeekDay do domínio. Começa na SEGUNDA, igual ao WeekRange do
// backend — e diferente do Date.getDay() do JS, que começa no domingo.
export type WeekDay =
  | 'Segunda'
  | 'Terca'
  | 'Quarta'
  | 'Quinta'
  | 'Sexta'
  | 'Sabado'
  | 'Domingo'

// Ordem de exibição, também a ordem em que o backend devolve.
export const WEEK_DAYS: readonly WeekDay[] = [
  'Segunda',
  'Terca',
  'Quarta',
  'Quinta',
  'Sexta',
  'Sabado',
  'Domingo',
]

export const WEEK_DAY_LABELS: Record<WeekDay, string> = {
  Segunda: 'Segunda',
  Terca: 'Terça',
  Quarta: 'Quarta',
  Quinta: 'Quinta',
  Sexta: 'Sexta',
  Sabado: 'Sábado',
  Domingo: 'Domingo',
}

/**
 * Dia da semana de hoje no formato do domínio.
 *
 * getDay() devolve 0 para domingo; o deslocamento +6 %7 converte para a
 * convenção do app, em que segunda é o índice 0. É a mesma conta que o
 * WeekRange faz no backend.
 */
export function todayWeekDay(): WeekDay {
  return WEEK_DAYS[(new Date().getDay() + 6) % 7]
}
