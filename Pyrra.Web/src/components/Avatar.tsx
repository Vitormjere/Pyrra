// Avatar compartilhado — mostra a foto de perfil quando existe, senão cai pro círculo com a
// inicial do nome (comportamento de sempre). Um componente só, usado em todo lugar que hoje
// mostra essa inicial: sidebar, perfil (próprio e público), membros/ranking de time, chat
// (1-a-1 e de time), amigos e busca de usuários.

export type AvatarSize = 'xs' | 'sidebar' | 'list' | 'profile'

const DIMENSION_CLASSES: Record<AvatarSize, string> = {
  xs: 'size-5',
  sidebar: 'size-8',
  list: 'size-9',
  profile: 'size-16',
}

const INITIAL_CLASSES: Record<AvatarSize, string> = {
  xs: 'text-[9px] bg-surface-hi text-slate-300',
  sidebar: 'text-xs bg-surface text-slate-300',
  list: 'text-sm bg-surface-hi text-slate-300',
  profile: 'text-2xl bg-surface-hi text-ink',
}

interface AvatarProps {
  name: string
  imageUrl?: string | null
  size?: AvatarSize
  className?: string
}

export function Avatar({ name, imageUrl, size = 'list', className }: AvatarProps) {
  const dimension = DIMENSION_CLASSES[size]

  if (imageUrl) {
    return (
      <img
        src={imageUrl}
        alt=""
        aria-hidden="true"
        className={[dimension, 'shrink-0 rounded-full object-cover ring-1 ring-line', className]
          .filter(Boolean)
          .join(' ')}
      />
    )
  }

  return (
    <span
      aria-hidden="true"
      className={[
        'flex shrink-0 items-center justify-center rounded-full font-semibold ring-1 ring-line',
        dimension,
        INITIAL_CLASSES[size],
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {name ? name.charAt(0).toUpperCase() : '?'}
    </span>
  )
}

export default Avatar
