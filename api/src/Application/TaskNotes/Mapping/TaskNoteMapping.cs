using Application.TaskNotes.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.TaskNotes.Mapping
{
    public static class TaskNoteMapping
    {
        public static TaskNoteReadDto ToReadDto(this TaskNote entity)
            => new()
            {
                Id = entity.Id,
                TaskId = entity.TaskId,
                AuthorId = entity.AuthorId,
                Content = entity.Content.Value,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                RowVersion = entity.RowVersion
            };

        public static TaskNote ToEntity(this TaskNoteCreateDto dto)
            => TaskNote.Create(dto.TaskId, dto.AuthorId, NoteContent.Create(dto.Content));

        public static TaskNoteEditDto ToEditDto(this TaskNoteReadDto dto)
            => new()
            {
                Id = dto.Id,
                Content = dto.Content,
                RowVersion = dto.RowVersion,
            };

        public static TaskNoteDeleteDto ToDeleteDto(this TaskNoteReadDto dto)
            => new()
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion
            };
    }
}
