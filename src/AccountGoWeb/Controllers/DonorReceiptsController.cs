using Microsoft.AspNetCore.Mvc;

namespace AccountGoWeb.Controllers
{
    [Route("donor-receipts")]
    public class DonorReceiptsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;

        public DonorReceiptsController(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
        }

        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("pdf")]
        public async Task<IActionResult> Pdf(DateTime from, DateTime to)
        {
            var apiUrl = (_configuration["ApiUrl"] ?? Environment.GetEnvironmentVariable("APIURL") ?? "http://localhost:8001/api/").TrimEnd('/') + "/";
            using var client = _clientFactory.CreateClient();
            using var response = await client.GetAsync($"{apiUrl}donations/DonationReceiptsPdf?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode)
                return Content((await response.Content.ReadAsStringAsync()).Trim('"'), "text/plain");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return File(bytes, "application/pdf", $"donation-tax-receipts-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
        }
    }
}
