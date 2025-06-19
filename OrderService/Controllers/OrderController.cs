using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;

        public OrderController(OrderDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> Post([FromBody] OrderRequest request)
        {
            if (request.Quantity <= 0)
            {
                return BadRequest(new { error = "Invalid quantity" });
            }

            var order = new Order
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                OrderDate = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new OrderResponse
            {
                OrderId = order.Id,
                Message = $"Order placed successfully for product {order.ProductId} with quantity {order.Quantity}."
            });
        }

        public class OrderRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class OrderResponse
        {
            public int OrderId { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
} 