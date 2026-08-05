namespace Pyrra.Application.Common.Exceptions {
    // impede remover categorias que possuem desafios vinculados
    public class ChallengeCategoryInUseException : Exception {
        public ChallengeCategoryInUseException()
            : base("Esta categoria tem desafios vinculados e não pode ser removida.") { }
    }
}
