using Microsoft.AspNetCore.Mvc;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReseedController : ControllerBase
    {
        private readonly IReseedService _reseedService;

        public ReseedController(IReseedService reseedService)
        {
            _reseedService = reseedService;
        }

        /// <summary>
        /// Reseeds the database with initial sample data and manages QuickBooks invoices
        /// </summary>
        /// <returns>Reseed operation results</returns>
        [HttpPost]
        public async Task<IActionResult> ReseedDatabase()
        {
            try
            {
                var result = await _reseedService.ReseedDatabaseAsync();
                return Ok(new {
                    message = "Reseed operation completed",
                    details = result.Split('\n').ToList(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new {
                    error = "Reseed operation failed",
                    details = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Gets the current database status (record counts)
        /// </summary>
        /// <returns>Database status information</returns>
        [HttpGet("status")]
        public IActionResult GetDatabaseStatus()
        {
            try
            {
                // This would need to be implemented in the reseed service
                // For now, return a simple response
                return Ok(new {
                    message = "Database status endpoint",
                    note = "This endpoint could show current record counts",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new {
                    error = "Failed to get database status",
                    details = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}