using ECommerceMVC.Data;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly Hshop2023Context db;
        public KhachHangController(Hshop2023Context context) 
        {
            db = context;
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }
    }
}
