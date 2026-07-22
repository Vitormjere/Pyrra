// Espelha os DTOs de Pyrra.Api/Dtos/Tarefas.

export type TaskPriority = 'Baixa' | 'Media' | 'Alta' | 'Urgente'

// GET /api/tarefas
export interface TaskResponse {
  id: string
  title: string
  priority: TaskPriority
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string
  completed: boolean
  createdAt: string
}

// GET /api/tarefas/semana — pendentes atrasadas da semana informada.
export interface WeeklyTasksResponse {
  weekStart: string
  weekEnd: string
  tasks: TaskResponse[]
}
