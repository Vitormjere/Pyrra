import ZeloCard from '../../components/ZeloCard'

// reaproveita o ZeloCard inteiro — a lógica já vivia nele quando era um card da tela Hoje, o card já traz nome e ícone servindo de cabeçalho
export function Zelo() {
  return (
    <div className="flex flex-col gap-5">
      <ZeloCard />

      <p className="px-1 text-xs leading-relaxed text-slate-500">
        O Zelo responde com base nos seus próprios dados de foco, treino e
        alimentação. Quanto mais você registra, melhores ficam as respostas.
      </p>
    </div>
  )
}

export default Zelo
