using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HashtagsController : ControllerBase
{
    private readonly IHashtagService _hashtagService;

    public HashtagsController(IHashtagService hashtagService)
    {
        _hashtagService = hashtagService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<HashtagResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var hashtags = await _hashtagService.GetAllAsync();
        var hashtagDtos = hashtags.Select(h => new HashtagResponseDto
        {
            Id = h.Id,
            Name = h.Name
        }).ToList();

        return this.SuccessResponse(hashtagDtos);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<HashtagResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var hashtag = await _hashtagService.GetByIdAsync(id);
        if (hashtag == null)
            return this.NotFoundResponse("Hashtag not found");

        var hashtagDto = new HashtagResponseDto
        {
            Id = hashtag.Id,
            Name = hashtag.Name
        };

        return this.SuccessResponse(hashtagDto);
    }

    [HttpGet("search/{name}")]
    [ProducesResponseType(typeof(ApiResponse<HashtagResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByName(string name)
    {
        var hashtag = await _hashtagService.GetByNameAsync(name);
        if (hashtag == null)
            return this.NotFoundResponse("Hashtag not found");

        var hashtagDto = new HashtagResponseDto
        {
            Id = hashtag.Id,
            Name = hashtag.Name
        };

        return this.SuccessResponse(hashtagDto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<HashtagResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateHashtagDto createHashtagDto)
    {
        var hashtag = new Hashtag
        {
            Name = createHashtagDto.Name
        };

        var created = await _hashtagService.CreateAsync(hashtag);

        var hashtagDto = new HashtagResponseDto
        {
            Id = created.Id,
            Name = created.Name
        };

        return this.CreatedResponse(hashtagDto);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _hashtagService.DeleteAsync(id);
        if (!result)
            return this.NotFoundResponse("Hashtag not found");

        return this.SuccessResponse(statusCode: 204);
    }
}
