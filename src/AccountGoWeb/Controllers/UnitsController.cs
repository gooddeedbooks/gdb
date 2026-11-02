using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    /// <summary>Hosts the inventory units management pages.</summary>
    [Route("units")]
    public class UnitsController : Controller
    {
        /// <summary>Displays the units list.</summary>
        /// <returns>The units list view.</returns>
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>Displays the new or edit unit form.</summary>
        /// <param name="id">The unit identifier, or zero for a new unit.</param>
        /// <returns>The unit form view.</returns>
        [HttpGet("new")]
        [HttpGet("{id:int:min(1)}")]
        public IActionResult Unit(int id = 0)
        {
            return View(id);
        }
    }
}
