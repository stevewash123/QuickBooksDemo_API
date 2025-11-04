using Microsoft.AspNetCore.Mvc;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Models.Enums;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobs()
    {
        var jobs = await _jobService.GetAllJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetJob(string id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<JobDto>> CreateJob(JobDto jobDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Basic validation
            if (string.IsNullOrEmpty(jobDto.CustomerId))
            {
                return BadRequest("CustomerId is required");
            }

            if (string.IsNullOrEmpty(jobDto.Description))
            {
                return BadRequest("Description is required");
            }

            var job = await _jobService.CreateJobAsync(jobDto);
            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating job: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JobDto>> UpdateJob(string id, JobDto jobDto)
    {
        var job = await _jobService.UpdateJobAsync(id, jobDto);
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(string id)
    {
        var result = await _jobService.DeleteJobAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobsByCustomer(string customerId)
    {
        var jobs = await _jobService.GetJobsByCustomerAsync(customerId);
        return Ok(jobs);
    }

    [HttpGet("technician/{technicianId}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobsByTechnician(string technicianId)
    {
        var jobs = await _jobService.GetJobsByTechnicianAsync(technicianId);
        return Ok(jobs);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobsByStatus(JobStatus status)
    {
        var jobs = await _jobService.GetJobsByStatusAsync(status);
        return Ok(jobs);
    }

    [HttpGet("type/{jobType}")]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobsByType(JobType jobType)
    {
        var jobs = await _jobService.GetJobsByTypeAsync(jobType);
        return Ok(jobs);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<JobDto>>> SearchJobs([FromQuery] string term)
    {
        var jobs = await _jobService.SearchJobsAsync(term);
        return Ok(jobs);
    }
}