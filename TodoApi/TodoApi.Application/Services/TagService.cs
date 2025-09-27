using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Application.Interfaces;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Interfaces;

namespace TodoApi.Application.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITodoItemRepository _todoItemRepository;
        private readonly IMapper _mapper;

        public TagService(ITagRepository tagRepository, ITodoItemRepository todoItemRepository, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _todoItemRepository = todoItemRepository;
            _mapper = mapper;
        }

        public async Task<TagDto> CreateTagAsync(TagDto tagDto)
        {
            var entity = _mapper.Map<Tag>(tagDto);
            await _tagRepository.AddAsync(entity);
            return _mapper.Map<TagDto>(entity);
        }

        public async Task<TagDto> GetTagByIdAsync(int id)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with id {id} was not found.");
            }

            return _mapper.Map<TagDto>(tag);
        }

        public async Task<IEnumerable<TagDto>> GetAllTagsAsync()
        {
            var tags = await _tagRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<TagDto>>(tags);
        }

        public async Task UpdateTagAsync(int id, TagDto tagDto)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with id {id} was not found.");
            }

            tag.Name = tagDto.Name;
            await _tagRepository.UpdateAsync(tag);
        }

        public async Task DeleteTagAsync(int id)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with id {id} was not found.");
            }

            await _tagRepository.DeleteAsync(tag);
        }

        public async Task<IEnumerable<TagDto>> GetTagsForTodoAsync(int todoItemId)
        {
            var todoItem = await _todoItemRepository.GetByIdAsync(todoItemId);
            if (todoItem == null)
            {
                throw new KeyNotFoundException($"Todo item with id {todoItemId} was not found.");
            }

            var tags = await _tagRepository.GetTagsForTodoAsync(todoItemId);
            return _mapper.Map<IEnumerable<TagDto>>(tags);
        }

        public async Task AttachTagToTodoAsync(int todoItemId, int tagId)
        {
            var todo = await _todoItemRepository.GetByIdAsync(todoItemId);
            if (todo == null)
            {
                throw new KeyNotFoundException($"Todo item with id {todoItemId} was not found.");
            }

            var tag = await _tagRepository.GetByIdAsync(tagId);
            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with id {tagId} was not found.");
            }

            await _tagRepository.AttachTagToTodoAsync(todoItemId, tagId);
        }

        public async Task DetachTagFromTodoAsync(int todoItemId, int tagId)
        {
            await _tagRepository.DetachTagFromTodoAsync(todoItemId, tagId);
        }
    }
}
