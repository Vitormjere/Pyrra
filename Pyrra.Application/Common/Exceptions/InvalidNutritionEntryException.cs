using System;

namespace Pyrra.Application.Common.Exceptions {
    public class InvalidNutritionEntryException : Exception {
        public InvalidNutritionEntryException(string message) : base(message) { }
    }
}
