using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    [Route("donor-transactions")]
    public class DonorTransactionsController : Controller
    {
        [HttpGet("")]
        public IActionResult Index(int donorId = 0)
        {
            return View(donorId);
        }

        [HttpGet("new")]
        [HttpGet("{id:int:min(1)}")]
        public IActionResult Transaction(int id = 0, int donorId = 0)
        {
            ViewBag.DonorId = donorId;
            return View(id);
        }

        [HttpGet("{id:int:min(1)}/receipt")]
        public IActionResult Receipt(int id)
        {
            return View(id);
        }
    }
}
