import ZeloCard from '../../components/ZeloCard'

// Tela dedicada do Zelo. Reaproveita o ZeloCard inteiro (campo, botão, loading,
// resposta e tratamento de erro) — a lógica já vivia nele quando era um card da
// tela Hoje, então mover para uma rota própria é só passá-lo a ser o conteúdo
// principal da página. O próprio card já traz o nome "Zelo" e o ícone, servindo
// de cabeçalho da tela.
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
