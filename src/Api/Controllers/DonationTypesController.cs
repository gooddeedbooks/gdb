using Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DonationTypeDto = Dto.DonationTypes.DonationType;
using DonationTypeEntity = Core.Domain.Donations.DonationType;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/donation-types")]
    public class DonationTypesController : BaseController
    {
        private readonly ApiDbContext _context;

        public DonationTypesController(ApiDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DonationTypeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DonationTypeDto>>> GetDonationTypesAsync(CancellationToken cancellationToken)
        {
            var types = await _context.DonationTypes.AsNoTracking().Include(type => type.GlAccount).OrderBy(type => type.Name).ToListAsync(cancellationToken);
            return Ok(types.Select(ToDto).ToList());
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DonationTypeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DonationTypeDto>> GetDonationTypeAsync(int id, CancellationToken cancellationToken)
        {
            var type = await _context.DonationTypes.AsNoTracking().Include(entity => entity.GlAccount).FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
            if (type == null)
                return NotFound();

            return Ok(ToDto(type));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DonationTypeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DonationTypeDto>> CreateDonationTypeAsync(DonationTypeDto input, CancellationToken cancellationToken)
        {
            var type = new DonationTypeEntity();
            var invalid = await ApplyInputAsync(type, input, cancellationToken);
            if (invalid != null)
                return invalid;
            _context.DonationTypes.Add(type);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(type).Reference(entity => entity.GlAccount).LoadAsync(cancellationToken);
            return CreatedAtAction("GetDonationType", new { id = type.Id }, ToDto(type));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDonationTypeAsync(int id, DonationTypeDto input, CancellationToken cancellationToken)
        {
            var type = await _context.DonationTypes.FindAsync(new object[] { id }, cancellationToken);
            if (type == null)
                return NotFound();

            var invalid = await ApplyInputAsync(type, input, cancellationToken);
            if (invalid != null)
                return invalid;
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDonationTypeAsync(int id, CancellationToken cancellationToken)
        {
            var type = await _context.DonationTypes.FindAsync(new object[] { id }, cancellationToken);
            if (type == null)
                return NotFound();

            _context.DonationTypes.Remove(type);
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private async Task<ActionResult> ApplyInputAsync(DonationTypeEntity type, DonationTypeDto input, CancellationToken cancellationToken)
        {
            if (input.GlAccountId.HasValue)
            {
                var account = await _context.Accounts.AsNoTracking()
                    .FirstOrDefaultAsync(entity => entity.Id == input.GlAccountId.Value, cancellationToken);
                if (account == null || !int.TryParse(account.AccountCode, out var code) || code < 41001 || code > 41999)
                {
                    ModelState.AddModelError(nameof(input.GlAccountId), "G/L Code must be an account numbered 41001 to 41999.");
                    return ValidationProblem(ModelState);
                }
            }

            type.Name = input.Name.Trim();
            type.Description = (input.Description ?? string.Empty).Trim();
            type.GlAccountId = input.GlAccountId;
            return null;
        }

        private static DonationTypeDto ToDto(DonationTypeEntity type)
        {
            return new DonationTypeDto
            {
                Id = type.Id,
                Name = type.Name,
                Description = type.Description,
                GlAccountId = type.GlAccountId,
                GlAccountCode = type.GlAccount?.AccountCode ?? string.Empty,
                GlAccountName = type.GlAccount?.AccountName ?? string.Empty
            };
        }
    }
}
