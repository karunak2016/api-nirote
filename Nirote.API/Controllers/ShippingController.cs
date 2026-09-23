using Nirote.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shipping;
    private readonly ILogger<ShippingController> _logger;

    public ShippingController(IShippingService shipping, ILogger<ShippingController> logger)
    {
        _shipping = shipping;
        _logger = logger;
    }

    [HttpGet("serviceability")]
    public async Task<IActionResult> CheckServiceability([FromQuery] string pincode)
    {
        try
        {
            _logger.LogInformation("Check serviceability for pincode {Pincode}", pincode);
            var serviceable = await _shipping.CheckServiceabilityAsync(pincode);
            return Ok(new { serviceable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check serviceability failed for pincode {Pincode}", pincode);
            throw;
        }
    }

    [HttpPost("create-shipment")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateShipment([FromBody] int orderId)
    {
        try
        {
            _logger.LogInformation("Create shipment for order {OrderId}", orderId);
            var awbCode = await _shipping.CreateShipmentAsync(orderId);
            _logger.LogInformation("Shipment created for order {OrderId} AWB={AWBCode}", orderId, awbCode);
            return Ok(new { awbCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create shipment failed for order {OrderId}", orderId);
            throw;
        }
    }

    [HttpGet("track/{awbCode}")]
    [Authorize]
    public async Task<IActionResult> Track(string awbCode)
    {
        try
        {
            _logger.LogInformation("Track shipment AWB={AWBCode}", awbCode);
            return Ok(await _shipping.TrackShipmentAsync(awbCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track shipment failed for AWB={AWBCode}", awbCode);
            throw;
        }
    }
}
