using Microsoft.EntityFrameworkCore;
using QuickBooksDemo.DAL.Context;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Models.Entities;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Service.Implementations;

public class TechnicianService : ITechnicianService
{
    private readonly QuickBooksDemoContext _context;

    public TechnicianService(QuickBooksDemoContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TechnicianDto>> GetAllTechniciansAsync()
    {
        var technicians = await _context.Technicians.ToListAsync();
        return technicians.Select(MapToDto);
    }

    public async Task<TechnicianDto?> GetTechnicianByIdAsync(string id)
    {
        var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.Id == id);
        return technician != null ? MapToDto(technician) : null;
    }

    public async Task<TechnicianDto> CreateTechnicianAsync(TechnicianDto technicianDto)
    {
        var technician = MapToEntity(technicianDto);
        technician.Id = Guid.NewGuid().ToString("N")[..8]; // Generate short ID

        _context.Technicians.Add(technician);
        await _context.SaveChangesAsync();

        return MapToDto(technician);
    }

    public async Task<TechnicianDto?> UpdateTechnicianAsync(string id, TechnicianDto technicianDto)
    {
        var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.Id == id);
        if (technician == null) return null;

        technician.Name = technicianDto.Name;
        technician.Phone = technicianDto.Phone;
        technician.Email = technicianDto.Email;
        technician.Specialties = technicianDto.Specialties;
        technician.Active = technicianDto.Active;

        await _context.SaveChangesAsync();
        return MapToDto(technician);
    }

    public async Task<bool> DeleteTechnicianAsync(string id)
    {
        var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.Id == id);
        if (technician == null) return false;

        _context.Technicians.Remove(technician);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<TechnicianDto>> GetActiveTechniciansAsync()
    {
        var technicians = await _context.Technicians
            .Where(t => t.Active)
            .ToListAsync();

        return technicians.Select(MapToDto);
    }

    private static TechnicianDto MapToDto(Technician technician)
    {
        return new TechnicianDto
        {
            Id = technician.Id,
            Name = technician.Name,
            Phone = technician.Phone,
            Email = technician.Email,
            Specialties = technician.Specialties,
            Active = technician.Active
        };
    }

    private static Technician MapToEntity(TechnicianDto dto)
    {
        return new Technician
        {
            Id = dto.Id,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Specialties = dto.Specialties,
            Active = dto.Active
        };
    }
}