import { useCallback, useRef, useState } from 'react'
import ConfirmDialog from '../components/ConfirmDialog'

interface ConfirmOptions {
  title: string
  message?: string
  confirmLabel?: string
  cancelLabel?: string
  destructive?: boolean
}

// Ponte entre o fluxo síncrono do antigo window.confirm e um modal controlado por
// estado: confirm(opts) abre o diálogo e devolve uma Promise<boolean> que resolve
// quando o usuário decide. Assim os handlers mantêm a forma
// `if (!(await confirm(...))) return`, quase idêntica ao `if (!window.confirm())`.
//
// Uso: `const { confirm, dialog } = useConfirm()`, renderize `{dialog}` na tela e
// chame `await confirm({ title, message, ... })` onde antes havia window.confirm.
export function useConfirm() {
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const resolverRef = useRef<((value: boolean) => void) | null>(null)

  const confirm = useCallback((opts: ConfirmOptions): Promise<boolean> => {
    setOptions(opts)
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve
    })
  }, [])

  const settle = useCallback((result: boolean) => {
    resolverRef.current?.(result)
    resolverRef.current = null
    setOptions(null)
  }, [])

  const dialog = options ? (
    <ConfirmDialog
      title={options.title}
      message={options.message}
      confirmLabel={options.confirmLabel}
      cancelLabel={options.cancelLabel}
      destructive={options.destructive}
      onConfirm={() => settle(true)}
      onCancel={() => settle(false)}
    />
  ) : null

  return { confirm, dialog }
}

export default useConfirm
