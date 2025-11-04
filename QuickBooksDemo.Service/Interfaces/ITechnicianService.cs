using QuickBooksDemo.Models.DTOs;

namespace QuickBooksDemo.Service.Interfaces;

public interface ITechnicianService
{
    Task<IEnumerable<TechnicianDto>> GetAllTechniciansAsync();
    Task<TechnicianDto?> GetTechnicianByIdAsync(string id);
    Task<TechnicianDto> CreateTechnicianAsync(TechnicianDto technicianDto);
    Task<TechnicianDto?> UpdateTechnicianAsync(string id, TechnicianDto technicianDto);
    Task<bool> DeleteTechnicianAsync(string id);
    Task<IEnumerable<TechnicianDto>> GetActiveTechniciansAsync();
}