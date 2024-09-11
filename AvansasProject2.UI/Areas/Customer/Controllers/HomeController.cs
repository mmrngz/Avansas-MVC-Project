using AvansasProject2.DAL.Data;
using AvansasProject2.MODEL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;

namespace AvansasProject.UI.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        
        public readonly ILogger<HomeController> _logger;
        public readonly ApplicationDbContext _context; 


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
        {
            
            _logger = logger;
            _context = dbContext;    
        }
        public IActionResult Search(string q)
        {
            if (!String.IsNullOrEmpty(q))
            {
                var ara = _context.Products.Where(i => i.Title.Contains(q) || i.Description.Contains(q));
                return View(ara);
            }
            return View();
        }
        public IActionResult CategoryDetails(int? id)
        {
            var product = _context.Products.Where(i => i.CategoryId == id).ToList();
            ViewBag.KategoriId = id;
            return View(product);
        }
        public IActionResult Index()
        {
            var products = _context.Products.Where(i => i.IsHome).ToList();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _context.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).ToList().Count();
                HttpContext.Session.SetInt32(Other.ssShoppingCard, count);
            }
            return View(products);
        }
        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(i => i.Id == id);
            ShoppingCard cart = new ShoppingCard()
            {
                Product = product,
                ProductId = product.Id
            };
            return View(cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCard Scart)
        {
            Scart.Id = 0;
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                Scart.ApplicationUserId = claim.Value;
                ShoppingCard cart = _context.ShoppingCards.FirstOrDefault(
                    u => u.ApplicationUserId == Scart.ApplicationUserId && u.ProductId == Scart.ProductId);
                if (cart == null)
                {
                    _context.ShoppingCards.Add(Scart);
                }
                else
                {
                    cart.Count += Scart.Count;
                }
                _context.SaveChanges();
                var count = _context.ShoppingCards.Where(i => i.ApplicationUserId == Scart.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(Other.ssShoppingCard, count);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var product = _context.Products.FirstOrDefault(i => i.Id == Scart.Id);
                ShoppingCard cart = new ShoppingCard()
                {
                    Product = product,
                    ProductId = product.Id
                };
            }

            return View(Scart);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
