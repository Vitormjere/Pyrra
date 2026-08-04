import {
  Area,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import SectionHeader from './SectionHeader'
import EmptyState from './EmptyState'
import { formatCurrency, formatShortDate } from '../utils/format'
import type { DailyBalanceResponse } from '../types/finance'

interface BalanceChartProps {
  history: DailyBalanceResponse[]
  days: number
}

// "1,2 mil" ocupa bem menos espaço que "R$ 1.234,56" — eixo Y estreito não comporta a moeda inteira
const axisFormatter = new Intl.NumberFormat('pt-BR', {
  notation: 'compact',
  maximumFractionDigits: 1,
})

export function BalanceChart({ history, days }: BalanceChartProps) {
  // uma linha exige pelo menos dois pontos, com menos o gráfico ficaria vazio sem explicar por quê
  if (history.length < 2) {
    return (
      <section className="flex flex-col gap-2">
        <SectionHeader>Saldo nos últimos {days} dias</SectionHeader>
        <EmptyState title="Ainda não há dados suficientes para o gráfico." />
      </section>
    )
  }

  const data = history.map((point) => ({
    date: point.date,
    label: formatShortDate(point.date),
    balance: point.balance,
  }))

  return (
    <section className="flex flex-col gap-2">
      <SectionHeader>Saldo nos últimos {days} dias</SectionHeader>

      <div className="rounded-md bg-surface py-4 pr-4 ring-1 ring-line">
        {/* Altura fixa no contêiner: o ResponsiveContainer precisa de uma altura
            concreta do pai para calcular a sua. */}
        <div className="h-52 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
              {/* Gradiente vertical do glow sob a linha: verde translúcido colado
                  à linha (topo) esmaecendo até transparente na base. id referenciado
                  pelo fill da Area abaixo. */}
              <defs>
                <linearGradient id="balanceGlow" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#02F5A1" stopOpacity={0.4} />
                  <stop offset="100%" stopColor="#02F5A1" stopOpacity={0} />
                </linearGradient>
              </defs>

              {/* Só linhas horizontais: as verticais competiriam com a própria
                  série num gráfico de 30 pontos. */}
              <CartesianGrid
                stroke="#1C282C"
                strokeDasharray="3 3"
                vertical={false}
              />
              <XAxis
                dataKey="label"
                tick={{ fill: '#64748b', fontSize: 10 }}
                tickLine={false}
                axisLine={false}
                // mostra ~5 rótulos em vez de 30 sobrepostos
                interval={Math.max(0, Math.floor(data.length / 5) - 1)}
                minTickGap={8}
              />
              <YAxis
                tick={{ fill: '#64748b', fontSize: 10 }}
                tickLine={false}
                axisLine={false}
                width={44}
                tickFormatter={(value: number) => axisFormatter.format(value)}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#0D1417',
                  border: '1px solid #1C282C',
                  borderRadius: 6,
                  fontSize: 12,
                }}
                labelStyle={{ color: '#94a3b8' }}
                itemStyle={{ color: '#e2e8f0' }}
                // recharts admite valores não numéricos, então converte explícito em vez de assumir number
                formatter={(value) => [formatCurrency(Number(value)), 'Saldo']}
              />
              {/* Glow em degradê sob a linha. Sem traço próprio (stroke=none) e
                  antes da Line no DOM, para a linha verde ficar por cima. Mesmo
                  type/dataKey da Line, então a base da área acompanha a curva.
                  activeDot=false: o ponto de hover é responsabilidade da Line. */}
              <Area
                type="monotone"
                dataKey="balance"
                stroke="none"
                fill="url(#balanceGlow)"
                activeDot={false}
                isAnimationActive={false}
              />
              <Line
                type="monotone"
                dataKey="balance"
                stroke="#02F5A1"
                strokeWidth={2}
                // glow inline porque o recharts renderiza a linha no próprio SVG, fora do alcance de classe do contêiner
                style={{
                  filter: 'drop-shadow(0 0 6px rgb(2 245 161 / 0.55))',
                }}
                // sem bolinha por ponto — com 30 pontos vira poluição visual, só o ponto do cursor aparece
                dot={false}
                activeDot={{ r: 4, fill: '#02F5A1' }}
              />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      </div>
    </section>
  )
}

export default BalanceChart
