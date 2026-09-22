using AutoMapper;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs;

namespace InteractHub.API.Mapping;

public class PostReportProfile : Profile
{
    public PostReportProfile()
    {
        // PostReport -> PostReportResponseDto
        CreateMap<PostReport, PostReportResponseDto>()
            .ForMember(dest => dest.ReporterUser, opt => opt.MapFrom(src => src.ReporterUser))
            .ForMember(dest => dest.ReviewedByAdmin, opt => opt.MapFrom(src => src.ReviewedByAdmin))
            .ForMember(dest => dest.Post, opt => opt.MapFrom(src => src.Post));
    }
}
