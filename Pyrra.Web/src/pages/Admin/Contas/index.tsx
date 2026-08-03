import { UserCog } from 'lucide-react'
import EmptyState from '../../../components/EmptyState'

// Placeholder da Fase Admin-1 — gestão de contas chega na Admin-2.
export function AdminContas() {
  return (
    <div className="flex flex-col gap-5">
      <h1 className="glow-ink font-display text-3xl font-semibold tracking-tight text-ink">
        Contas
      </h1>
      <EmptyState
        icon={UserCog}
        title="Em construção."
        description="A gestão de contas chega numa próxima etapa."
      />
    </div>
  )
}

export default AdminContas
