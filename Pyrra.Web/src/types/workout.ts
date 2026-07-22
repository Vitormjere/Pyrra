// Espelha os DTOs de Pyrra.Api/Dtos/Treinos.

export type WorkoutType = 'Academia' | 'Corrida'

// GET /api/treinos
// Os campos das duas modalidades convivem no mesmo registro e só um conjunto vem
// preenchido: Academia usa exerciseName/loadKg/sets/reps, Corrida usa
// distanceKm/durationMinutes/paceMinPerKm. Quem garante a coerência é o backend.
export interface WorkoutResponse {
  id: string
  type: WorkoutType
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string

  exerciseName: string | null
  loadKg: number | null
  sets: number | null
  reps: number | null

  distanceKm: number | null
  durationMinutes: number | null
  paceMinPerKm: number | null

  notes: string | null
  createdAt: string
}
