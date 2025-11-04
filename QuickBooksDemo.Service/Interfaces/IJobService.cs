using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Models.Enums;

namespace QuickBooksDemo.Service.Interfaces;

public interface IJobService
{
    Task<IEnumerable<JobDto>> GetAllJobsAsync();
    Task<JobDto?> GetJobByIdAsync(string id);
    Task<JobDto> CreateJobAsync(JobDto jobDto);
    Task<JobDto?> UpdateJobAsync(string id, JobDto jobDto);
    Task<bool> DeleteJobAsync(string id);
    Task<IEnumerable<JobDto>> GetJobsByCustomerAsync(string customerId);
    Task<IEnumerable<JobDto>> GetJobsByTechnicianAsync(string technicianId);
    Task<IEnumerable<JobDto>> GetJobsByStatusAsync(JobStatus status);
    Task<IEnumerable<JobDto>> GetJobsByTypeAsync(JobType jobType);
    Task<IEnumerable<JobDto>> SearchJobsAsync(string searchTerm);
}