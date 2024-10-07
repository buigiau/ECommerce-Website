using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using ECommerceMVC.Helpers;
using Microsoft.AspNetCore.Authorization;
using ECommerceMVC.Services;

namespace ECommerceMVC.Controllers
{
	public class CartController : Controller
	{
		private readonly PaypalClient _paypalClient;
		public readonly Hshop2023Context db;
		private readonly IVnPayService _vnPayService;

		public CartController(Hshop2023Context context, PaypalClient paypalClient, IVnPayService vnPayService)
		{
			_paypalClient = paypalClient;
			db = context;
			_vnPayService = vnPayService;
		}
		public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
		public IActionResult Index()
		{
			return View(Cart);
		}

		public IActionResult AddToCart(int id, int quantity = 1)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHh == id);
			if (item == null)
			{
				var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
				if (hangHoa == null)
				{
					TempData["Message"] = $"Can't found the product with id {id}";
					return Redirect("/404");
				}
				item = new CartItem
				{
					MaHh = hangHoa.MaHh,
					TenHH = hangHoa.TenHh,
					DonGia = hangHoa.DonGia ?? 0,
					Hinh = hangHoa.Hinh ?? string.Empty,
					SoLuong = quantity,
				};
				gioHang.Add(item);
			}

			else
			{
				item.SoLuong += quantity;
			}

			HttpContext.Session.Set(MySetting.CART_KEY, gioHang);

			return RedirectToAction("Index");
		}

		public IActionResult RemoveCart(int id)
		{
			var gioHang = Cart;
			var item = gioHang.SingleOrDefault(p => p.MaHh == id);
			if (item != null)
			{
				gioHang.Remove(item);
				HttpContext.Session.Set(MySetting.CART_KEY, gioHang);
			}
			return RedirectToAction("Index");
		}

		[Authorize]
		[HttpGet]
		public IActionResult Checkout()
		{
			if (Cart.Count == 0)
			{
				return Redirect("/");
			}

			ViewBag.PaypalClientId = _paypalClient.ClientId;
			return View(Cart);
		}

		[Authorize]
		[HttpPost]
		public IActionResult Checkout(CheckoutVM model, string payment = "COD")
		{
			if(payment=="Thanh Toán VNPay")
			{
				var vnPayModel = new VnPaymentRequestModel
				{
					Amount = Cart.Sum(p => p.ThanhTien),
					CreatedDate = DateTime.Now,
					Description = $"{model.HoTen} {model.DienThoai}",
					FullName = model.HoTen,
					OrderId = new Random().Next(1000, 10000)
				};
				return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
			}
			if (ModelState.IsValid)
			{
				var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMER_ID).Value;
				var khachHang = new KhachHang();
				if (model.GiongKhachHang)
				{
					khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == customerId);
				}

				var hoadon = new HoaDon
				{
					MaKh = customerId,
					HoTen = model.HoTen ?? khachHang.HoTen,
					DiaChi = model.DiaChi ?? khachHang.DiaChi,
					DienThoai = model.DienThoai ?? khachHang.DienThoai,
					NgayDat = DateTime.Now,
					CachThanhToan = "COD",
					CachVanChuyen = "GRAB",
					MaTrangThai = 0,
					GhiChu = model.GhiChu
				};

				db.Database.BeginTransaction();
				try
				{
					db.Database.CommitTransaction();
					db.Add(hoadon);
					db.SaveChanges();

					var cthds = new List<ChiTietHd>();
					foreach (var item in Cart)
					{
						cthds.Add(new ChiTietHd
						{
							MaHd = hoadon.MaHd,
							SoLuong = item.SoLuong,
							DonGia = item.DonGia,
							MaHh = item.MaHh,
							GiamGia = 0
						});
					}
					db.AddRange(cthds);
					db.SaveChanges();

					HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());

					return View("Success");
				}
				catch
				{
					db.Database.RollbackTransaction();
				}
			}

			return View(Cart);
		}


		#region Paypal payment
		[Authorize]
		[HttpPost("/Cart/create-paypal-order")]
		public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
		{
			// Thông tin đơn hàng gửi qua Paypal
			var tongTien = Cart.Sum(p => p.ThanhTien).ToString();
			var donViTienTe = "USD";
			var maDonHangThamChieu = "DH" + DateTime.Now.Ticks.ToString();

			try
			{
				var response = await _paypalClient.CreateOrder(tongTien, donViTienTe, maDonHangThamChieu);

				return Ok(response);
			}
			catch (Exception ex)
			{
				var error = new { ex.GetBaseException().Message };
				return BadRequest(error);
			}
		}

		[Authorize]
		[HttpPost("/Cart/capture-paypal-order")]
		public async Task<IActionResult> CapturePaypalOrder(string orderID, CancellationToken cancellationToken)
		{
			try
			{
				var response = await _paypalClient.CaptureOrder(orderID);

				// Lưu database đơn hàng của mình

				return Ok(response);
			}
			catch (Exception ex)
			{
				var error = new { ex.GetBaseException().Message };
				return BadRequest(error);
			}
		}

		#endregion

		[Authorize]
		public IActionResult PaymentSuccess()
		{
			return View("Success");
		}

		[Authorize]
		public IActionResult PaymentFail() 
		{
			return View();
		}	

		[Authorize]
		public IActionResult PaymentCallBack()
		{
			var response = _vnPayService.PaymentExecute(Request.Query);
			if(response == null || response.VnPayResponseCode != "00")
			{
				TempData["Message"] = $"Lỗi thanh toán VNPAY: {response.VnPayResponseCode}";
				return RedirectToAction("PaymentFail");
			}

			//Lưu đơn hàng vào db


			TempData["Message"] = $"Thanh toán VNPAY thành công!!!";
			return RedirectToAction("PaymentSuccess");
		}
	}
}
