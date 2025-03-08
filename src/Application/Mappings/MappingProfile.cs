using AutoMapper;
using DMS.Auth.Application.Dtos;
using DMS.Auth.Domain.Entities;

namespace DMS.Auth.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserDto, User>()
            .ReverseMap();       
    }
}
