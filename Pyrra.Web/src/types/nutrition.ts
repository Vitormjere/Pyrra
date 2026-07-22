// Espelha os DTOs de Pyrra.Api/Dtos/Nutricao.

// Valores exatos do enum MealType do backend — sem underscores. A ORDEM é
// cronológica e o backend devolve os grupos nela, então preservá-la aqui mantém
// a tela na mesma sequência do dia.
export type MealType = 'CafeDaManha' | 'Almoco' | 'Lanche' | 'Jantar'

export interface NutritionItemResponse {
  id: string
  itemName: string
  /** Texto livre: "2 ovos", "1 prato". Sem unidade estruturada, por decisão de produto. */
  quantity: string
  createdAt: string
}

// As quatro refeições vêm SEMPRE, mesmo vazias — o backend garante isso, então a
// tela não precisa completar buracos.
export interface MealGroupResponse {
  meal: MealType
  items: NutritionItemResponse[]
}

// GET /api/nutricao
export interface DayNutritionResponse {
  /** DateOnly serializado como "YYYY-MM-DD". */
  date: string
  meals: MealGroupResponse[]
}

// GET /api/nutricao/semana — os 7 dias sempre presentes, mesmo os sem item.
export interface WeekNutritionResponse {
  weekStart: string
  weekEnd: string
  days: DayNutritionResponse[]
}
