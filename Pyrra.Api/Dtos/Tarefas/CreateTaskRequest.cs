using System;
using System.ComponentModel.DataAnnotations;
using Pyrra.Domain.Tarefas;

namespace Pyrra.Api.Dtos.Tarefas {
    public record CreateTaskRequest(
        [Required][MaxLength(500)] string Title,
        [Required] TaskPriority? Priority,
        DateOnly? Date = null);
}
