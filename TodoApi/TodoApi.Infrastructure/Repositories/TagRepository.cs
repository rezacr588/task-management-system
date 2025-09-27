using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly ApplicationDbContext _context;

        public TagRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tags.FindAsync(id);
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            return await _context.Tags.AsNoTracking().ToListAsync();
        }

        public async Task AddAsync(Tag tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tag tag)
        {
            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Tag tag)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsForTodoAsync(int todoItemId)
        {
            return await _context.TodoItems
                .Where(t => t.Id == todoItemId)
                .SelectMany(t => t.Tags)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AttachTagToTodoAsync(int todoItemId, int tagId)
        {
            var todoItem = await _context.TodoItems
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == todoItemId)
                ?? throw new KeyNotFoundException($"Todo item with id {todoItemId} was not found.");

            var tag = await _context.Tags.FindAsync(tagId)
                ?? throw new KeyNotFoundException($"Tag with id {tagId} was not found.");

            if (todoItem.Tags.All(t => t.Id != tagId))
            {
                todoItem.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DetachTagFromTodoAsync(int todoItemId, int tagId)
        {
            var todoItem = await _context.TodoItems
                .Include(t => t.Tags)
                .FirstOrDefaultAsync(t => t.Id == todoItemId)
                ?? throw new KeyNotFoundException($"Todo item with id {todoItemId} was not found.");

            var tag = todoItem.Tags.FirstOrDefault(t => t.Id == tagId)
                ?? throw new KeyNotFoundException($"Tag with id {tagId} is not associated with todo item {todoItemId}.");

            todoItem.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }
}
