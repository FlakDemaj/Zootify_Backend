using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Anonym.Response;
using Zootify_Backend.Model.Services.Anonym.Interface;

namespace Zootify_Backend.Controllers.Anonym;

[AllowAnonymous]
public class AnonymSpecimenImageController : BaseRestController
{
    private readonly IAnonymSpecimenImageService _anonymSpecimenImageService;

    public AnonymSpecimenImageController(IAnonymSpecimenImageService anonymSpecimenImageService)
    {
        _anonymSpecimenImageService = anonymSpecimenImageService;
    }

    [HttpGet("{imageGuid:guid}")]
    public async Task<ActionResult<GetImageBase64ResponseDto>> GetSpecimenImageAsync(Guid imageGuid)
    {
        return Ok(await _anonymSpecimenImageService.GetImageBase64Async(imageGuid));
    }
}