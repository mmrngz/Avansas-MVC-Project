﻿using AvansasProject2.DAL.Data;
using AvansasProject2.MODEL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Iyzipay.Model;
using Iyzipay.Request;
using System.Collections.Generic;
using Iyzipay;

namespace AvansasProject2.UI.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        [BindProperty]

        public CartController(UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ApplicationDbContext db)
        {
            _db = db;
            _emailSender = emailSender;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var ShoppingCardVM = new ShoppingCardVM()
            {
                OrderHeader = new MODEL.Models.OrderHeader(),
                ListCard = _db.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product)
            };
            ShoppingCardVM.OrderHeader.OrderTotal = 0;
            ShoppingCardVM.OrderHeader.ApplicationUser = _db.ApplicationUsers.FirstOrDefault(i => i.Id == claim.Value);
            foreach (var item in ShoppingCardVM.ListCard)
            {
                ShoppingCardVM.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
            }
            return View(ShoppingCardVM);
        }
        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _db.ApplicationUsers.FirstOrDefault(i => i.Id == claim.Value);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Dogrulama emaili Bos..");
            }
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            ModelState.AddModelError(string.Empty, "Email dogrulama Kodu Gonder..");
            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
        public IActionResult Add(int cartId)
        {
            var cart = _db.ShoppingCards.FirstOrDefault(i => i.Id == cartId);
            cart.Count += 1;
            _db.SaveChanges();  
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Decrease(int cartId)
        {
            var cart = _db.ShoppingCards.FirstOrDefault(i => i.Id == cartId);
            if (cart.Count == 1)
            {
                var count = _db.ShoppingCards.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
                _db.ShoppingCards.Remove(cart);
                _db.SaveChanges();
                HttpContext.Session.SetInt32(Other.ssShoppingCard, count - 1);
            }
            else
            {
                cart.Count -= 1;
                _db.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            var cart = _db.ShoppingCards.FirstOrDefault(i => i.Id == cartId);

            var count = _db.ShoppingCards.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
            _db.ShoppingCards.Remove(cart);
            _db.SaveChanges();
            HttpContext.Session.SetInt32(Other.ssShoppingCard, count - 1);


            return RedirectToAction(nameof(Index));
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCardVM = new ShoppingCardVM()
            {
                OrderHeader = new MODEL.Models.OrderHeader(),
                ListCard = _db.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product)
            };
            foreach (var item in ShoppingCardVM.ListCard)
            {
                item.Price = item.Product.Price;
                ShoppingCardVM.OrderHeader.OrderTotal += (item.Count * item.Product.Price);
            }
            return View(ShoppingCardVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Summary(ShoppingCardVM model)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCardVM.ListCard = _db.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product);
            ShoppingCardVM.OrderHeader.OrderStatus = Other.Durum_Beklemede;
            ShoppingCardVM.OrderHeader.ApplicationUserId = claim.Value;
            ShoppingCardVM.OrderHeader.OrderDate = DateTime.Now;
            _db.OrderHeaders.Add(ShoppingCardVM.OrderHeader);
            _db.SaveChanges();
            foreach (var item in ShoppingCardVM.ListCard)
            {
                item.Price = item.Product.Price;
                OrderDetails orderDetails = new OrderDetails()
                {
                    ProductId = item.ProductId,
                    OrderId = ShoppingCardVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };
                ShoppingCardVM.OrderHeader.OrderTotal += item.Count * item.Product.Price;
                model.OrderHeader.OrderTotal += item.Count * item.Product.Price;
                _db.OrderDetails.Add(orderDetails);
            }
            var payment = PaymentProcess(model);
            _db.ShoppingCards.RemoveRange(ShoppingCardVM.ListCard);
            _db.SaveChanges();
            HttpContext.Session.SetInt32(Other.ssShoppingCard, 0);
            return RedirectToAction("SiparisTamam");
        }
        public IActionResult SiparisTamam()
        {
            return View();
        }
        private Payment PaymentProcess(ShoppingCardVM model)
        {
            Options options = new Options();
            options.ApiKey = "sandbox-9XvfhoKqpgKY2e5RWhbk3IVUfyEkOmXz";
            options.SecretKey = "sandbox-RDk1s5x6KtK45RuIJGjQ4gAz8jYY9rM9";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = new Random().Next(1111, 9999).ToString();
            request.Price = model.OrderHeader.OrderTotal.ToString();
            request.PaidPrice = model.OrderHeader.OrderTotal.ToString();
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = "B67832";
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            //PaymentCard paymentCard = new PaymentCard();
            //paymentCard.CardHolderName = "John Doe";
            //paymentCard.CardNumber = "5528790000000008";
            //paymentCard.ExpireMonth = "12";
            //paymentCard.ExpireYear = "2030";
            //paymentCard.Cvc = "123";
            //paymentCard.RegisterCard = 0;
            //request.PaymentCard = paymentCard;

            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = model.OrderHeader.CartName;
            paymentCard.CardNumber = model.OrderHeader.CartNumber;
            paymentCard.ExpireMonth = model.OrderHeader.ExpirationMonth;
            paymentCard.ExpireYear = model.OrderHeader.ExpirationYear;
            paymentCard.Cvc = model.OrderHeader.Cvc;
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer();
            buyer.Id = model.OrderHeader.Id.ToString();
            buyer.Name = model.OrderHeader.Name;
            buyer.Surname = model.OrderHeader.Surname;
            buyer.GsmNumber = model.OrderHeader.PhoneNumber;
            buyer.Email = "email@email.com";
            buyer.IdentityNumber = "74300864791";
            buyer.LastLoginDate = "2015-10-05 12:43:35";
            buyer.RegistrationDate = "2013-04-21 15:12:09";
            buyer.RegistrationAddress = model.OrderHeader.Address;
            buyer.Ip = "85.34.78.112";
            buyer.City = model.OrderHeader.City;
            buyer.Country = "Turkey";
            buyer.ZipCode = model.OrderHeader.PostCode;
            request.Buyer = buyer;

            Address shippingAddress = new Address();
            shippingAddress.ContactName = "Jane Doe";
            shippingAddress.City = "Istanbul";
            shippingAddress.Country = "Turkey";
            shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            shippingAddress.ZipCode = "34742";
            request.ShippingAddress = shippingAddress;

            Address billingAddress = new Address();
            billingAddress.ContactName = "Jane Doe";
            billingAddress.City = "Istanbul";
            billingAddress.Country = "Turkey";
            billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            billingAddress.ZipCode = "34742";
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            foreach (var item in _db.ShoppingCards.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product))
            {
                basketItems.Add(new BasketItem()
                {

                    Id = item.Id.ToString(),
                    Name = item.Product.Title,
                    Category1 = item.Product.CategoryId.ToString(),
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = (item.Price * item.Count).ToString()
                });
            }
            request.BasketItems = basketItems;

            return Payment.Create(request, options);
        }
    }
}

