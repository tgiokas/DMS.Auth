using AutoMapper;
using Authentication.Application.Dtos;
using Authentication.Domain.Entities;

namespace Authentication.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //CreateMap<UserDto, User>()
        //    .ReverseMap();
        CreateMap<UserCreateDto, User>();
    }
}
