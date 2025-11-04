using Microsoft.EntityFrameworkCore;
using QuickBooksDemo.DAL.Context;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Models.Entities;
using QuickBooksDemo.Models.Enums;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Service.Implementations;

public class JobService : IJobService
{
    private readonly QuickBooksDemoContext _context;

    public JobService(QuickBooksDemoContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<JobDto>> GetAllJobsAsync()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .ToListAsync();

        return jobs.Select(MapToDto);
    }

    public async Task<JobDto?> GetJobByIdAsync(string id)
    {
        var job = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .FirstOrDefaultAsync(j => j.Id == id);

        return job != null ? MapToDto(job) : null;
    }

    public async Task<JobDto> CreateJobAsync(JobDto jobDto)
    {
        var job = MapToEntity(jobDto);
        job.Id = await GenerateJobIdAsync(); // Generate CCE_XXXX format ID
        job.CreatedDate = DateTime.Now;

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Reload with includes
        var createdJob = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .FirstAsync(j => j.Id == job.Id);

        return MapToDto(createdJob);
    }

    public async Task<JobDto?> UpdateJobAsync(string id, JobDto jobDto)
    {
        var job = await _context.Jobs
            .Include(j => j.LineItems)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return null;

        job.CustomerId = jobDto.CustomerId;
        job.Status = jobDto.Status;
        job.JobType = jobDto.JobType;
        job.Description = jobDto.Description;
        job.QuotedAmount = jobDto.QuotedAmount;
        job.ActualAmount = jobDto.ActualAmount;
        job.ScheduledDate = jobDto.ScheduledDate;
        job.CompletedDate = jobDto.CompletedDate;
        job.AssignedTechnicianId = jobDto.AssignedTechnicianId;

        // Update line items
        _context.LineItems.RemoveRange(job.LineItems);
        foreach (var lineItemDto in jobDto.LineItems)
        {
            var lineItem = new LineItem
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                JobId = job.Id,
                Description = lineItemDto.Description,
                MaterialCost = lineItemDto.MaterialCost,
                LaborHours = lineItemDto.LaborHours,
                LaborCost = lineItemDto.LaborCost
            };
            _context.LineItems.Add(lineItem);
        }

        await _context.SaveChangesAsync();

        // Reload with includes
        var updatedJob = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .FirstAsync(j => j.Id == id);

        return MapToDto(updatedJob);
    }

    public async Task<bool> DeleteJobAsync(string id)
    {
        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return false;

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<JobDto>> GetJobsByCustomerAsync(string customerId)
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .Where(j => j.CustomerId == customerId)
            .ToListAsync();

        return jobs.Select(MapToDto);
    }

    public async Task<IEnumerable<JobDto>> GetJobsByTechnicianAsync(string technicianId)
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .Where(j => j.AssignedTechnicianId == technicianId)
            .ToListAsync();

        return jobs.Select(MapToDto);
    }

    public async Task<IEnumerable<JobDto>> GetJobsByStatusAsync(JobStatus status)
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .Where(j => j.Status == status)
            .ToListAsync();

        return jobs.Select(MapToDto);
    }

    public async Task<IEnumerable<JobDto>> GetJobsByTypeAsync(JobType jobType)
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .Where(j => j.JobType == jobType)
            .ToListAsync();

        return jobs.Select(MapToDto);
    }

    public async Task<IEnumerable<JobDto>> SearchJobsAsync(string searchTerm)
    {
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.LineItems)
            .Where(j => j.Description.Contains(searchTerm) ||
                       j.Customer.Name.Contains(searchTerm) ||
                       (j.AssignedTechnician != null && j.AssignedTechnician.Name.Contains(searchTerm)))
            .ToListAsync();

        return jobs.Select(MapToDto);
    }

    private async Task<string> GenerateJobIdAsync()
    {
        // Get the current highest job number to increment
        var existingJobs = await _context.Jobs
            .Where(j => j.Id.StartsWith("CCE_"))
            .Select(j => j.Id)
            .ToListAsync();

        int nextNumber = 1;
        if (existingJobs.Any())
        {
            var numbers = existingJobs
                .Select(id => id.Replace("CCE_", ""))
                .Where(numStr => int.TryParse(numStr, out _))
                .Select(int.Parse)
                .ToList();

            if (numbers.Any())
            {
                nextNumber = numbers.Max() + 1;
            }
        }

        return $"CCE_{nextNumber:D4}";
    }

    private static JobDto MapToDto(Job job)
    {
        return new JobDto
        {
            Id = job.Id,
            CustomerId = job.CustomerId,
            CustomerName = job.Customer?.Name ?? "",
            Status = job.Status,
            JobType = job.JobType,
            Description = job.Description,
            QuotedAmount = job.QuotedAmount,
            ActualAmount = job.ActualAmount,
            CreatedDate = job.CreatedDate,
            ScheduledDate = job.ScheduledDate,
            CompletedDate = job.CompletedDate,
            AssignedTechnicianId = job.AssignedTechnicianId,
            AssignedTechnicianName = job.AssignedTechnician?.Name,
            LineItems = job.LineItems?.Select(li => new LineItemDto
            {
                Id = li.Id,
                Description = li.Description,
                MaterialCost = li.MaterialCost,
                LaborHours = li.LaborHours,
                LaborCost = li.LaborCost,
                TotalCost = li.TotalCost
            }).ToList() ?? new List<LineItemDto>(),
            TotalLineItemCost = job.TotalLineItemCost,
            TotalLaborHours = job.TotalLaborHours
        };
    }

    private static Job MapToEntity(JobDto dto)
    {
        return new Job
        {
            Id = dto.Id,
            CustomerId = dto.CustomerId,
            Status = dto.Status,
            JobType = dto.JobType,
            Description = dto.Description,
            QuotedAmount = dto.QuotedAmount,
            ActualAmount = dto.ActualAmount,
            CreatedDate = dto.CreatedDate,
            ScheduledDate = dto.ScheduledDate,
            CompletedDate = dto.CompletedDate,
            AssignedTechnicianId = dto.AssignedTechnicianId
        };
    }
}