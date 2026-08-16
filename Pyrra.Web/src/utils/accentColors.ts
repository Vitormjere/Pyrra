import type { AccentColor } from '../types/auth'

// Paleta pensada pra manter a mesma "temperatura" neon do verde atual (#02F5A1: saturação bem
// alta, luminosidade média) sobre o fundo quase preto (--color-brand-dark, #05090A). Tons mais
// frios (azul/roxo) ficam um pouco mais claros que o necessário "a olho" porque o canal azul
// pesa pouco na luminância percebida — sem esse ajuste, ficam escuros demais quando usados como
// cor de texto (text-brand-green em links, ícones ativos etc.).
export const ACCENT_COLOR_HEX: Record<AccentColor, string> = {
  Verde: '#02F5A1',
  Azul: '#3B9EFF',
  Rosa: '#FF2E9F',
  Roxo: '#B14EFF',
  Vermelho: '#FF4B4B',
  Laranja: '#FF8A1E',
  Amarelo: '#FFD400',
}

// Componentes R G B (espaço separado, sem vírgula) de cada hex acima — usado nos utilities de
// glow do index.css, que precisam montar um rgb(... / alpha) e não têm como derivar isso de um
// hex em CSS puro.
export const ACCENT_COLOR_RGB: Record<AccentColor, string> = {
  Verde: '2 245 161',
  Azul: '59 158 255',
  Rosa: '255 46 159',
  Roxo: '177 78 255',
  Vermelho: '255 75 75',
  Laranja: '255 138 30',
  Amarelo: '255 212 0',
}

export const ACCENT_COLOR_LABELS: Record<AccentColor, string> = {
  Verde: 'Verde',
  Azul: 'Azul',
  Rosa: 'Rosa',
  Roxo: 'Roxo',
  Vermelho: 'Vermelho',
  Laranja: 'Laranja',
  Amarelo: 'Amarelo',
}

export const ACCENT_COLOR_OPTIONS: readonly AccentColor[] = [
  'Verde',
  'Azul',
  'Rosa',
  'Roxo',
  'Vermelho',
  'Laranja',
  'Amarelo',
]

/**
 * Sobrescreve as CSS custom properties que todo o app usa pra cor de destaque
 * (--color-brand-green e --brand-green-rgb, ver index.css). O Tailwind v4 gera as utilities
 * bg-brand-green/text-brand-green/etc. como `var(--color-brand-green)`, então mudar essas duas
 * variáveis no :root já basta — nenhum componente precisa saber que a cor mudou.
 */
export function applyAccentColor(color: AccentColor): void {
  const root = document.documentElement
  root.style.setProperty('--color-brand-green', ACCENT_COLOR_HEX[color])
  root.style.setProperty('--brand-green-rgb', ACCENT_COLOR_RGB[color])
}
