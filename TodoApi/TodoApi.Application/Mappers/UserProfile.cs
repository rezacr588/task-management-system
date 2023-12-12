using AutoMapper;
using TodoApi.Application.DTOs;
using TodoApi.Domain.DTOs;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Mappers
{
  public class UserProfile : Profile
  {
    public UserProfile()
    {
      CreateMap<User, UserDto>().ReverseMap();
    }
  }
}
