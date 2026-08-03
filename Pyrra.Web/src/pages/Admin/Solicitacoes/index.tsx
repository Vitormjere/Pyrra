import { ClipboardList } from 'lucide-react'
import EmptyState from '../../../components/EmptyState'

// Placeholder da Fase Admin-1 — fila de solicitações chega na Admin-3.
export function AdminSolicitacoes() {
  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
        Solicitações
      </h1>
      <EmptyState
        icon={ClipboardList}
        title="Em construção."
        description="A fila de solicitações chega numa próxima etapa."
      />
    </div>
  )
}

export default AdminSolicitacoes
