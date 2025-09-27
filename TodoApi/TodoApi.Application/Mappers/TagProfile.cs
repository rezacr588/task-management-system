using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Mappers
{
    public class TagProfile : Profile
    {
        public TagProfile()
        {
            CreateMap<Tag, TagDto>().ReverseMap();
        }
    }
}
