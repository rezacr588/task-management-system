using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IMapper _mapper;

        public ActivityLogService(IActivityLogRepository activityLogRepository, IMapper mapper)
        {
            _activityLogRepository = activityLogRepository;
            _mapper = mapper;
        }

        public async Task<ActivityLogDto> GetActivityByIdAsync(int id)
        {
            var entry = await _activityLogRepository.GetByIdAsync(id);
            return _mapper.Map<ActivityLogDto>(entry);
        }

        public async Task<IEnumerable<ActivityLogDto>> GetActivityForTodoAsync(int todoItemId)
        {
            var entries = await _activityLogRepository.GetByTodoItemIdAsync(todoItemId);
            return entries.Select(entry => _mapper.Map<ActivityLogDto>(entry)).ToList();
        }

        public async Task<ActivityLogDto> RecordAsync(ActivityLogDto activity)
        {
            var entry = _mapper.Map<ActivityLogEntry>(activity);
            await _activityLogRepository.AddAsync(entry);
            return _mapper.Map<ActivityLogDto>(entry);
        }

        public async Task RecordRangeAsync(IEnumerable<ActivityLogDto> activities)
        {
            var entries = activities.Select(activity => _mapper.Map<ActivityLogEntry>(activity)).ToList();
            await _activityLogRepository.AddRangeAsync(entries);
        }
    }
}
