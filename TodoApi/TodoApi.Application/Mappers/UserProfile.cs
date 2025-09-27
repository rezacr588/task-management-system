using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Mappers
{
  public class UserProfile : Profile
  {
    public UserProfile()
    {
      CreateMap<User, UserDto>().ReverseMap();

      CreateMap<UserRegistrationDto, User>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
        .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.AssignedTodoItems, opt => opt.Ignore());

      CreateMap<UserUpdateDto, User>()
        .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.LastUpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
        .ForMember(dest => dest.AssignedTodoItems, opt => opt.Ignore());
    }
  }
}
