using System;
using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Mappers
{
    public class CollaborationProfile : Profile
    {
        public CollaborationProfile()
        {
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.AuthorDisplayName,
                    opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.AuthorDisplayName)
                        ? src.AuthorDisplayName
                        : src.Author != null
                            ? src.Author.Name
                            : null));

            CreateMap<CommentCreateRequest, Comment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore())
                .ForMember(dest => dest.TodoItem, opt => opt.Ignore());

            CreateMap<CommentUpdateRequest, Comment>()
                .ForAllMembers(opt => opt.Condition((src, _, value) => value != null));

            CreateMap<ActivityLogEntry, ActivityLogDto>()
                .ForMember(dest => dest.ActorDisplayName,
                    opt => opt.MapFrom(src => src.Actor != null ? src.Actor.Name : null));

            CreateMap<ActivityLogDto, ActivityLogEntry>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Actor, opt => opt.Ignore())
                .ForMember(dest => dest.TodoItem, opt => opt.Ignore())
                .ForMember(dest => dest.RelatedComment, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt == default ? DateTime.UtcNow : src.CreatedAt));
        }
    }
}
