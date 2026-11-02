using Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UnitDto = Dto.Inventory.Unit;
using Measurement = Core.Domain.Items.Measurement;

namespace Api.Controllers
{
    /// <summary>Manages the units used by inventory and transaction dropdowns.</summary>
    [ApiController]
    [Route("api/units")]
    public class UnitsController : BaseController
    {
        private readonly ApiDbContext _context;

        /// <summary>Creates the units controller.</summary>
        /// <param name="context">The application database context.</param>
        public UnitsController(ApiDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>Lists units ordered by description.</summary>
        /// <param name="cancellationToken">Cancels the database operation.</param>
        /// <returns>The existing measurement records.</returns>
        [HttpGet]
        public async Task<ActionResult<List<UnitDto>>> GetUnitsAsync(CancellationToken cancellationToken)
        {
            return Ok(await _context.Measurements.AsNoTracking().OrderBy(unit => unit.Description)
                .Select(unit => new UnitDto { Id = unit.Id, Code = unit.Code, Description = unit.Description })
                .ToListAsync(cancellationToken));
        }

        /// <summary>Gets a unit for editing.</summary>
        /// <param name="id">The unit identifier.</param>
        /// <param name="cancellationToken">Cancels the database operation.</param>
        /// <returns>The unit, or HTTP 404 if it no longer exists.</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UnitDto>> GetUnitAsync(int id, CancellationToken cancellationToken)
        {
            var unit = await _context.Measurements.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
            return unit == null ? NotFound() : Ok(ToDto(unit));
        }

        /// <summary>Creates a unit in the shared lookup.</summary>
        /// <param name="input">The code and description.</param>
        /// <param name="cancellationToken">Cancels the database operation.</param>
        /// <returns>The created unit, or a validation error.</returns>
        [HttpPost]
        public async Task<ActionResult<UnitDto>> CreateUnitAsync(UnitDto input, CancellationToken cancellationToken)
        {
            var invalid = await ValidateInputAsync(input, 0, cancellationToken);
            if (invalid != null) return invalid;
            var unit = new Measurement { Code = input.Code.Trim(), Description = input.Description.Trim() };
            _context.Measurements.Add(unit);
            await _context.SaveChangesAsync(cancellationToken);
            return CreatedAtAction("GetUnit", new { id = unit.Id }, ToDto(unit));
        }

        /// <summary>Updates a unit without changing its identifier or references.</summary>
        /// <param name="id">The unit identifier.</param>
        /// <param name="input">The updated code and description.</param>
        /// <param name="cancellationToken">Cancels the database operation.</param>
        /// <returns>HTTP 204, or an error if the input or identifier is invalid.</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUnitAsync(int id, UnitDto input, CancellationToken cancellationToken)
        {
            var unit = await _context.Measurements.FindAsync(new object[] { id }, cancellationToken);
            if (unit == null) return NotFound();
            var invalid = await ValidateInputAsync(input, id, cancellationToken);
            if (invalid != null) return invalid;
            unit.Code = input.Code.Trim();
            unit.Description = input.Description.Trim();
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        /// <summary>Deletes an unused unit; units referenced by items or transactions are retained.</summary>
        /// <param name="id">The unit identifier.</param>
        /// <param name="cancellationToken">Cancels the database operation.</param>
        /// <returns>HTTP 204, HTTP 404, or HTTP 409 when the unit is in use.</returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteUnitAsync(int id, CancellationToken cancellationToken)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                // Prevent a concurrent transaction from referencing a unit after the usage checks.
                await using var transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);
                var unit = await _context.Measurements.FindAsync(new object[] { id }, cancellationToken);
                if (unit == null) return NotFound();

                if (await IsInUseAsync(id, cancellationToken))
                    return Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Unit is in use",
                        Detail = "This unit is used by an item, item category, or transaction and cannot be deleted."
                    });

                _context.Measurements.Remove(unit);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return (IActionResult)NoContent();
            });
        }

        private async Task<ActionResult?> ValidateInputAsync(UnitDto input, int id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.Code))
                ModelState.AddModelError(nameof(input.Code), "Code is required. Enter the unit code.");
            if (string.IsNullOrWhiteSpace(input.Description))
                ModelState.AddModelError(nameof(input.Description), "Description is required. Enter the unit description.");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var code = input.Code.Trim().ToUpperInvariant();
            if (await _context.Measurements.AnyAsync(unit => unit.Id != id && unit.Code.Trim().ToUpper() == code, cancellationToken))
            {
                ModelState.AddModelError(nameof(input.Code), "A unit with this code already exists. Enter a different code.");
                return ValidationProblem(ModelState);
            }
            return null;
        }

        private async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken)
        {
            return await _context.Items.AnyAsync(item => item.SmallestMeasurementId == id ||
                       item.SellMeasurementId == id || item.PurchaseMeasurementId == id, cancellationToken)
                || await _context.ItemCategories.AnyAsync(item => item.MeasurementId == id, cancellationToken)
                || await _context.InventoryControlJournals.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.SalesQuoteLines.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.SalesOrderLines.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.SalesInvoiceLines.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.SalesDeliveryLines.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.SalesReceiptLines.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.PurchaseReceiptLines.AnyAsync(line => line.MeasurementId == id, cancellationToken)
                || await _context.DonationInvoiceLines.AnyAsync(line => line.MeasurementId == id, cancellationToken);
        }

        private static UnitDto ToDto(Measurement unit)
        {
            return new UnitDto { Id = unit.Id, Code = unit.Code, Description = unit.Description };
        }
    }
}
