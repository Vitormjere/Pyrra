using System;

namespace Pyrra.Domain.Focos {
    public class FreezeBank {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int FreezesAvailable { get; set; }

        // Segunda-feira da última semana em que um freeze foi concedido. Guardar o início da
        // semana (e não a data da concessão) torna o cálculo de "semanas completas passadas"
        // uma subtração simples, e mantém a concessão idempotente dentro da mesma semana.
        public DateOnly LastGrantedWeekStart { get; set; }
    }
}
