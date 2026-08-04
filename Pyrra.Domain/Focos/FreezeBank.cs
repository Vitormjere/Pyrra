using System;

namespace Pyrra.Domain.Focos {
    public class FreezeBank {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int FreezesAvailable { get; set; }

        // segunda-feira da última semana em que ganhou um freeze 
        public DateOnly LastGrantedWeekStart { get; set; }
    }
}
