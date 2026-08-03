import { MessageSquare } from 'lucide-react'
import EmptyState from '../../../components/EmptyState'

// Placeholder da Fase Admin-1 — mensagens chegam na Admin-4.
export function AdminMensagens() {
  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
        Mensagens
      </h1>
      <EmptyState
        icon={MessageSquare}
        title="Em construção."
        description="O envio de mensagens chega numa próxima etapa."
      />
    </div>
  )
}

export default AdminMensagens
