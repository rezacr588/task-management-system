using AutoMapper;
using TodoApi.Domain.Entities;
using TodoApi.Application.DTOs;

namespace TodoApi.Application.Mappings
{
    public class TodoItemProfile : Profile
    {
        public TodoItemProfile()
        {
            // Map from TodoItem (Domain Model) to TodoItemDto (Data Transfer Object)
            CreateMap<TodoItem, TodoItemDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsComplete, opt => opt.MapFrom(src => src.IsComplete))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.CompletedDate, opt => opt.MapFrom(src => src.CompletedDate))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.AssignedToUserId, opt => opt.MapFrom(src => src.AssignedToUserId))
                // Add other properties as necessary
                .ReverseMap(); // If you need to map from TodoItemDto to TodoItem as well

            // Create reverse mapping if necessary
            // This is useful for scenarios like creating/updating TodoItem from TodoItemDto
            CreateMap<TodoItemDto, TodoItem>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsComplete, opt => opt.MapFrom(src => src.IsComplete))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
                .ForMember(dest => dest.CompletedDate, opt => opt.MapFrom(src => src.CompletedDate))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.AssignedToUserId, opt => opt.MapFrom(src => src.AssignedToUserId))
                // Add other properties as necessary
                ;
        }
    }
}
