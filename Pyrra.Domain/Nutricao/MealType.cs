namespace Pyrra.Domain.Nutricao {
    // A ordem é cronológica de propósito: as telas agrupam por refeição usando a ordem do enum,
    // então reordenar aqui reordena o dia inteiro no frontend.
    public enum MealType {
        CafeDaManha,
        Almoco,
        Lanche,
        Jantar
    }
}
