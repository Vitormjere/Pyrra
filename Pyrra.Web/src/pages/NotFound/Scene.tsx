import { Canvas, useFrame } from '@react-three/fiber'
import { OrbitControls } from '@react-three/drei'
import { useRef } from 'react'
import type { Mesh } from 'three'

// Icosaedro em wireframe, cor da marca — abstrato de propósito, não é ilustração literal de erro.
// Detail=1 mantém a malha em ~80 triângulos: leve o bastante pra não pesar em celular fraco.
function GlitchIcosahedron() {
  const meshRef = useRef<Mesh>(null)
  const startRef = useRef<number | null>(null)

  useFrame((_state, delta) => {
    const mesh = meshRef.current
    if (!mesh) return

    if (startRef.current === null) startRef.current = 0
    startRef.current += delta

    const elapsed = startRef.current

    // Entrada: escala com leve overshoot/tremor nos primeiros ~0.8s — o "glitch" pedido,
    // sem exagerar (some completamente depois de acomodar em 1).
    if (elapsed < 0.8) {
      const t = elapsed / 0.8
      const settle = 1 - (1 - t) ** 3
      const jitter = Math.sin(t * Math.PI * 6) * (1 - t) * 0.08
      mesh.scale.setScalar(Math.max(settle + jitter, 0))
    } else {
      mesh.scale.setScalar(1)
    }

    // Idle: rotação lenta e contínua, independente do auto-rotate da câmera (OrbitControls).
    mesh.rotation.x += delta * 0.18
    mesh.rotation.y += delta * 0.26
  })

  return (
    <mesh ref={meshRef} scale={0}>
      <icosahedronGeometry args={[1.4, 1]} />
      <meshStandardMaterial
        color="#02F5A1"
        emissive="#02F5A1"
        emissiveIntensity={0.5}
        wireframe
        transparent
        opacity={0.85}
      />
    </mesh>
  )
}

export function NotFoundScene() {
  return (
    <Canvas
      dpr={[1, 1.5]}
      gl={{ antialias: false }}
      camera={{ position: [0, 0, 4.2], fov: 45 }}
    >
      <ambientLight intensity={0.35} />
      <pointLight position={[3, 2, 4]} intensity={1.2} color="#02F5A1" />
      <GlitchIcosahedron />
      <OrbitControls enableZoom={false} enablePan={false} autoRotate autoRotateSpeed={0.6} />
    </Canvas>
  )
}

export default NotFoundScene
