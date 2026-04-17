using BarberShop.Application.DTOs.Blocks;
using BarberShop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberShop.API.Controllers;

[Authorize]
public class BlocksController(BlockService blockService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await blockService.GetAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBlockRequest request)
    {
        var result = await blockService.CreateAsync(GetUserId(), request);
        return Created(string.Empty, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await blockService.DeleteAsync(GetUserId(), id);
        return NoContent();
    }
}