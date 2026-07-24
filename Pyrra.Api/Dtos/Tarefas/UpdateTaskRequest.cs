using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Api.Dtos.Tarefas {
    public record UpdateTaskRequest(
        [Required][MaxLength(500)] string Title,
        [Required] TaskPriority? Priority);
}
