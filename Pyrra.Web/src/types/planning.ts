// Espelha os DTOs de Pyrra.Api/Dtos/Planejamento.

// GET /api/planejamento e /historico, PUT /api/planejamento
export interface PlanNoteResponse {
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string
  content: string
  /** null quando o dia nunca teve nota salva — é o que distingue
   *  "ainda não escreveu" de "escreveu e apagou". */
  updatedAt: string | null
}
