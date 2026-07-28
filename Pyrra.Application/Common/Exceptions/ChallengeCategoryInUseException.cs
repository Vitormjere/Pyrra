namespace Pyrra.Application.Common.Exceptions {
    // Categoria com desafios vinculados não pode ser removida: apagar deixaria os desafios
    // apontando para um id inexistente. Mesmo espírito do CategoryInUseException de Finanças.
    // Vira 409 no controller.
    public class ChallengeCategoryInUseException : Exception {
        public ChallengeCategoryInUseException()
            : base("Esta categoria tem desafios vinculados e não pode ser removida.") { }
    }
}
