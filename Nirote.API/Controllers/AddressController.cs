using System.Security.Claims;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize(Roles = "Customer,Admin")]
public class AddressController : ControllerBase
{
    private readonly AddressRepository _repo;
    private readonly ILogger<AddressController> _logger;

    public AddressController(AddressRepository repo, ILogger<AddressController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Get addresses for user {UserId}", UserId);
            return Ok(await _repo.GetByUserAsync(UserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get addresses failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(AddressUpsertDto dto)
    {
        try
        {
            _logger.LogInformation("Create address for user {UserId}", UserId);
            var id = await _repo.CreateAsync(new Address
            {
                UserId = UserId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Line1 = dto.Line1,
                Line2 = dto.Line2,
                City = dto.City,
                State = dto.State,
                Pincode = dto.Pincode,
                Country = dto.Country,
                IsDefault = dto.IsDefault
            });
            _logger.LogInformation("Address {AddressId} created for user {UserId}", id, UserId);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create address failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpPost("update/{id:int}")]
    public async Task<IActionResult> Update(int id, AddressUpsertDto dto)
    {
        try
        {
            _logger.LogInformation("Update address {AddressId} for user {UserId}", id, UserId);
            await _repo.UpdateAsync(new Address
            {
                Id = id,
                UserId = UserId,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Line1 = dto.Line1,
                Line2 = dto.Line2,
                City = dto.City,
                State = dto.State,
                Pincode = dto.Pincode,
                Country = dto.Country,
                IsDefault = dto.IsDefault
            });
            _logger.LogInformation("Address {AddressId} updated for user {UserId}", id, UserId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update address {AddressId} failed for user {UserId}", id, UserId);
            throw;
        }
    }

    [HttpPost("default/{id:int}")]
    public async Task<IActionResult> SetDefault(int id)
    {
        await _repo.SetDefaultAsync(id, UserId);
        return Ok();
    }

    [HttpPost("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("Delete address {AddressId} for user {UserId}", id, UserId);
            var error = await _repo.DeleteAsync(id);
            if (!string.IsNullOrEmpty(error)) return BadRequest(new { error });
            _logger.LogInformation("Address {AddressId} deleted for user {UserId}", id, UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete address {AddressId} failed for user {UserId}", id, UserId);
            throw;
        }
    }
}

public record AddressUpsertDto(
    string FullName, string Phone,
    string Line1, string? Line2,
    string City, string State, string Pincode,
    string Country,
    bool IsDefault);
