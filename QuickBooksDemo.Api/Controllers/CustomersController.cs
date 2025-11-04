using Microsoft.AspNetCore.Mvc;
using QuickBooksDemo.Models.DTOs;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(string id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CustomerDto customerDto)
    {
        var customer = await _customerService.CreateCustomerAsync(customerDto);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(string id, CustomerDto customerDto)
    {
        var customer = await _customerService.UpdateCustomerAsync(id, customerDto);
        if (customer == null)
        {
            return NotFound();
        }
        return Ok(customer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(string id)
    {
        var result = await _customerService.DeleteCustomerAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> SearchCustomers([FromQuery] string term)
    {
        var customers = await _customerService.SearchCustomersAsync(term);
        return Ok(customers);
    }
}