using Microsoft.AspNetCore.Mvc;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _technicianService;

    public TechniciansController(ITechnicianService technicianService)
    {
        _technicianService = technicianService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TechnicianDto>>> GetTechnicians()
    {
        var technicians = await _technicianService.GetAllTechniciansAsync();
        return Ok(technicians);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<TechnicianDto>>> GetActiveTechnicians()
    {
        var technicians = await _technicianService.GetActiveTechniciansAsync();
        return Ok(technicians);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TechnicianDto>> GetTechnician(string id)
    {
        var technician = await _technicianService.GetTechnicianByIdAsync(id);
        if (technician == null)
        {
            return NotFound();
        }
        return Ok(technician);
    }

    [HttpPost]
    public async Task<ActionResult<TechnicianDto>> CreateTechnician(TechnicianDto technicianDto)
    {
        var technician = await _technicianService.CreateTechnicianAsync(technicianDto);
        return CreatedAtAction(nameof(GetTechnician), new { id = technician.Id }, technician);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TechnicianDto>> UpdateTechnician(string id, TechnicianDto technicianDto)
    {
        var technician = await _technicianService.UpdateTechnicianAsync(id, technicianDto);
        if (technician == null)
        {
            return NotFound();
        }
        return Ok(technician);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTechnician(string id)
    {
        var result = await _technicianService.DeleteTechnicianAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}