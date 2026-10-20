using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    [Route("donation-types")]
    public class DonationTypesController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("new")]
        [HttpGet("{id:int:min(1)}")]
        public IActionResult DonationType(int id = 0)
        {
            return View(id);
        }
    }
}
