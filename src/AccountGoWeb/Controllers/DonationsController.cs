using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    // Donations are managed under Donors > Manage Donors; these routes keep old links working.
    [Route("donations")]
    public class DonationsController : Controller
    {
        [HttpGet("")]
        [HttpGet("donationinvoices")]
        public IActionResult Index()
        {
            return Redirect("/donors");
        }
    }
}
