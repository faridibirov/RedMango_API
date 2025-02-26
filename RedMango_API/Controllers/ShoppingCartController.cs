using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Models;
using System.Net;
using System.Diagnostics.Eventing.Reader;

namespace RedMango_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShoppingCartController : ControllerBase
{
    protected ApiResponse _response;

    private readonly ApplicationDbContext _db;

    public ShoppingCartController(ApplicationDbContext db)
    {
        _db = db;
        _response = new();
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
    {
        try
        {
            ShoppingCart shoppingCart;

            if (string.IsNullOrEmpty(userId))
            {
                shoppingCart = new();
            }

            else
            {
                shoppingCart = _db.ShoppingCarts.Include(sc => sc.CartItems).ThenInclude(sc => sc.MenuItem)
                   .FirstOrDefault(sc => sc.UserId == userId);
            }

            if (shoppingCart.CartItems != null && shoppingCart.CartItems.Count > 0)
            {

                shoppingCart.CartTotal = shoppingCart.CartItems.Sum(u => u.Quantity * u.MenuItem.Price);
            }

            _response.Result = shoppingCart;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
        catch (Exception ex)
        {
            _response.IsSuccess = false;
            _response.ErrorMessages = new List<string>() { ex.ToString() };
            _response.StatusCode = HttpStatusCode.BadRequest;
        }

        return _response;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int menuItemId, int updateQunatityBy)
    {
        ShoppingCart shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems).FirstOrDefault(u => u.UserId == userId);
        MenuItem menuItem = _db.MenuItems.FirstOrDefault(u => u.Id == menuItemId);

        if (menuItem == null)
        {
            _response.StatusCode = HttpStatusCode.BadRequest;
            _response.IsSuccess = false;
            return BadRequest(_response);
        }

        if (shoppingCart == null && updateQunatityBy > 0)
        {
            //create a shopping cart & add cart item

            ShoppingCart newCart = new() { UserId = userId };
            _db.ShoppingCarts.Add(newCart);
            _db.SaveChanges();

            CartItem newCartItem = new()
            {
                MenuItemId = menuItemId,
                Quantity = updateQunatityBy,
                ShoppingCartId = newCart.Id,
                MenuItem = null
            };

            _db.CartItems.Add(newCartItem);
            _db.SaveChanges();
        }

        else
        {
            //shopping cart exists

            CartItem cartItemInCart = shoppingCart.CartItems.FirstOrDefault(u => u.MenuItemId == menuItemId);

            if (cartItemInCart == null)
            {
                // item does not exist in current cart
                CartItem newCartItem = new()
                {
                    MenuItemId = menuItemId,
                    Quantity = updateQunatityBy,
                    ShoppingCartId = shoppingCart.Id,
                    MenuItem = null
                };
                _db.CartItems.Add(newCartItem);
                _db.SaveChanges();
            }
            else
            {
                //item already exist in the cart and we have to update quantity

                int newQuantity = cartItemInCart.Quantity + updateQunatityBy;

                if (updateQunatityBy == 0 || newQuantity <= 0)
                {
                    //remove cart item from cart and if it is the only item then remove cart
                    _db.CartItems.Remove(cartItemInCart);
                    if (shoppingCart.CartItems.Count() == 1)
                    {
                        _db.ShoppingCarts.Remove(shoppingCart);
                    }
                    _db.SaveChanges();
                }
                else
                {
                    cartItemInCart.Quantity = newQuantity;
                    _db.SaveChanges();
                }
            }
        }

        return _response;
    }

}
