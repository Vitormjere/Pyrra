import PyrraLogo from './PyrraLogo'

interface SplashProps {
  /** true dispara o fade de saída — o componente continua montado até o pai remover, pra a transição tocar. */
  fadingOut?: boolean
}

// Tela cheia preta com a logo centralizada e um brilho verde suave pulsando atrás dela —
// só transform/opacity via CSS (ver index.css), sem lib de animação. Cobre o tempo real
// de checagem de sessão no ProtectedRoute; não é um delay artificial.
export function Splash({ fadingOut = false }: SplashProps) {
  return (
    <div
      className={[
        'fixed inset-0 z-50 flex items-center justify-center overflow-hidden bg-brand-dark transition-opacity duration-500 ease-out',
        fadingOut ? 'pointer-events-none opacity-0' : 'opacity-100',
      ].join(' ')}
      role="status"
      aria-label="Carregando"
    >
      {/* brilho atrás do card: um glow largo e suave + um núcleo menor, ambos pulsando */}
      <div aria-hidden="true" className="pointer-events-none absolute inset-0 flex items-center justify-center">
        <div className="animate-pyrra-pulse absolute size-96 rounded-full bg-brand-green/20 blur-3xl" />
        <div className="animate-pyrra-pulse-core absolute size-56 rounded-full bg-brand-green/25 blur-2xl" />
      </div>

      <PyrraLogo size={168} className="relative" showText={false} />
    </div>
  )
}

export default Splash
