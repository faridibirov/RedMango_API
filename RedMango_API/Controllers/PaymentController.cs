using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using RedMango_API.Services;
using Stripe;
using System.Net;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private ApiResponse _response;
    public PaymentController(ApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
        _response = new ApiResponse();
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> MakePayment(string userId)
    {
        var shoppingCart = _db.ShoppingCarts.Include(sc => sc.CartItems).ThenInclude(sc => sc.MenuItem)
            .FirstOrDefault(sc => sc.UserId == userId);

        if (shoppingCart == null || shoppingCart.CartItems==null || shoppingCart.CartItems.Count()==0)
        {
            _response.IsSuccess = false;
            _response.StatusCode = HttpStatusCode.BadRequest;
            return BadRequest(_response);
        }

        #region Create Payment Intent
        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

        shoppingCart.CartTotal = shoppingCart.CartItems.Sum(u => u.Quantity * u.MenuItem.Price);

            var options = new PaymentIntentCreateOptions
            {
                Amount = (int)(shoppingCart.CartTotal * 100),
                Currency = "usd",
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
            };

        var service = new PaymentIntentService();
       var response = service.Create(options);

        shoppingCart.StripePaymentIntentId = response.Id;
        shoppingCart.ClientSecret = response.ClientSecret;
        #endregion

        _response.Result = shoppingCart;
        _response.StatusCode = HttpStatusCode.OK;
        return Ok(_response);

    }
}
