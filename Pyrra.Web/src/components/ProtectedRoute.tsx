import { useEffect, useRef, useState } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import Splash from './Splash'

// tempo do fade de saída da splash — precisa bater com a duration do Splash (index.css)
const SPLASH_FADE_MS = 500
// mínimo visível mesmo com sessão já resolvida rápido, senão a animação nem chega a aparecer
const SPLASH_MIN_VISIBLE_MS = 1800

// envolve as rotas que exigem sessão, usando <Outlet /> pra não repetir o guard em cada página
export function ProtectedRoute() {
  const { user, loading } = useAuth()
  const [showSplash, setShowSplash] = useState(true)
  const [fadingOut, setFadingOut] = useState(false)
  const mountedAtRef = useRef(Date.now())

  // a checagem de sessão (loading) É o carregamento real que a splash cobre — assim que
  // termina (e o mínimo visível já passou), começa o fade e só desmonta a splash depois da
  // transição tocar. Depende só de `loading`: se fadingOut/showSplash entrassem nas deps, o
  // próprio setFadingOut(true) reexecutaria o efeito e o cleanup cancelaria os timeouts antes
  // de disparar, travando a splash em opacity-0 pra sempre.
  useEffect(() => {
    if (loading) return

    const elapsed = Date.now() - mountedAtRef.current
    const remaining = Math.max(0, SPLASH_MIN_VISIBLE_MS - elapsed)

    let hideTimeout: ReturnType<typeof setTimeout> | undefined
    const startFadeTimeout = setTimeout(() => {
      setFadingOut(true)
      hideTimeout = setTimeout(() => setShowSplash(false), SPLASH_FADE_MS)
    }, remaining)

    return () => {
      clearTimeout(startFadeTimeout)
      if (hideTimeout) clearTimeout(hideTimeout)
    }
  }, [loading])

  if (showSplash) {
    return <Splash fadingOut={fadingOut} />
  }

  // replace pra não empilhar no histórico, senão o botão voltar levaria de novo à rota protegida
  return user ? <Outlet /> : <Navigate to="/login" replace />
}

export default ProtectedRoute
