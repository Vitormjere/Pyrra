namespace Pyrra.Domain.Common {
    /// <summary>
    /// Dia da semana, começando na SEGUNDA — igual ao WeekRange, que define a semana do
    /// sistema como segunda a domingo.
    ///
    /// Enum próprio em vez do System.DayOfWeek do .NET por dois motivos: aquele começa no
    /// domingo (Sunday = 0), o que criaria duas noções de "primeiro dia" convivendo no mesmo
    /// projeto; e ele serializaria como "Monday"/"Tuesday", destoando dos enums em português
    /// que o resto do domínio usa (Academia, CafeDaManha, Urgente).
    ///
    /// Compartilhado por Treinos e Nutrição, daí morar em Common.
    /// </summary>
    public enum WeekDay {
        Segunda,
        Terca,
        Quarta,
        Quinta,
        Sexta,
        Sabado,
        Domingo
    }
}
