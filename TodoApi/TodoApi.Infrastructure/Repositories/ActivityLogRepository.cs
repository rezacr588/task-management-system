using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;
using TodoApi.Infrastructure.Data;

namespace TodoApi.Infrastructure.Repositories
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActivityLogEntry> GetByIdAsync(int id)
        {
            return await _context.ActivityLogEntries
                .Include(a => a.Actor)
                .Include(a => a.RelatedComment)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new KeyNotFoundException($"Activity log entry with id {id} was not found.");
        }

        public async Task<IEnumerable<ActivityLogEntry>> GetByTodoItemIdAsync(int todoItemId)
        {
            return await _context.ActivityLogEntries
                .Include(a => a.Actor)
                .Include(a => a.RelatedComment)
                .Where(a => a.TodoItemId == todoItemId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(ActivityLogEntry entry)
        {
            _context.ActivityLogEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ActivityLogEntry> entries)
        {
            _context.ActivityLogEntries.AddRange(entries);
            await _context.SaveChangesAsync();
        }
    }
}
