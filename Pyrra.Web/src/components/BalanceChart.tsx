import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import SectionHeader from './SectionHeader'
import { formatCurrency, formatShortDate } from '../utils/format'
import type { DailyBalanceResponse } from '../types/finance'

interface BalanceChartProps {
  history: DailyBalanceResponse[]
  days: number
}

// Valores compactos no eixo: "1,2 mil" ocupa muito menos que "R$ 1.234,56" e o
// eixo Y de um gráfico estreito não comporta a moeda inteira.
const axisFormatter = new Intl.NumberFormat('pt-BR', {
  notation: 'compact',
  maximumFractionDigits: 1,
})

export function BalanceChart({ history, days }: BalanceChartProps) {
  // Uma linha exige pelo menos dois pontos; com menos, o gráfico renderizaria
  // uma área vazia sem explicar por quê.
  if (history.length < 2) {
    return (
      <section className="flex flex-col gap-2">
        <SectionHeader>Saldo nos últimos {days} dias</SectionHeader>
        <div className="rounded-md bg-surface px-5 py-8 text-center ring-1 ring-line">
          <p className="text-sm text-slate-400">
            Ainda não há dados suficientes para o gráfico.
          </p>
        </div>
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
            <LineChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
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
                // Mostra ~5 rótulos em vez de 30 sobrepostos.
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
                // O tipo do recharts admite valores não numéricos, então a
                // conversão é explícita em vez de assumir number.
                formatter={(value) => [formatCurrency(Number(value)), 'Saldo']}
              />
              <Line
                type="monotone"
                dataKey="balance"
                stroke="#02F5A1"
                strokeWidth={2}
                // Glow inline e não pela utility: o recharts renderiza a linha
                // dentro do seu próprio SVG, fora do alcance de uma classe
                // aplicada ao contêiner React.
                style={{
                  filter: 'drop-shadow(0 0 6px rgb(2 245 161 / 0.55))',
                }}
                // Sem bolinha por ponto: com 30 pontos vira uma fileira de
                // pontos que polui a leitura. Só o ponto sob o cursor aparece.
                dot={false}
                activeDot={{ r: 4, fill: '#02F5A1' }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>
    </section>
  )
}

export default BalanceChart
