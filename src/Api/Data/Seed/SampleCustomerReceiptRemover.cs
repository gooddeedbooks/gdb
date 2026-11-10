using Core.Domain;
using Core.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Api.Data.Seed
{
    /// <summary>Removes the sample customer payment that older builds seeded, along with its allocation and ledger entries.</summary>
    public static class SampleCustomerReceiptRemover
    {
        /// <summary>The amount of the payment that older builds seeded for the first demo invoice.</summary>
        public const decimal SampleAmount = 300m;

        private const string SamplePaymentMethod = "Cheque";
        private const string SampleMemoPrefix = "Payment for invoice #";

        /// <summary>Deletes seeded sample customer payments. Payments entered by users are never touched.</summary>
        /// <param name="context">The application database context.</param>
        /// <returns>The number of receipts removed.</returns>
        public static int RemoveSampleReceipts(ApiDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var receipts = context.SalesReceiptHeaders
                .Include(r => r.SalesReceiptLines)
                .Include(r => r.GeneralLedgerHeader!).ThenInclude(g => g.GeneralLedgerLines)
                .Where(r => r.Amount == SampleAmount && (r.PaymentMethod == null || r.PaymentMethod == SamplePaymentMethod))
                .AsEnumerable()
                .Where(IsSampleReceipt)
                .ToList();

            foreach (var receipt in receipts)
            {
                var allocations = context.CustomerAllocations.Where(a => a.SalesReceiptHeaderId == receipt.Id).ToList();
                foreach (var allocation in allocations)
                {
                    RemoveAllocationLedgerEntry(context, allocation);
                }

                context.CustomerAllocations.RemoveRange(allocations);
                RemoveLedgerHeader(context, receipt.GeneralLedgerHeader);
                context.SalesReceiptLines.RemoveRange(receipt.SalesReceiptLines);
                context.SalesReceiptHeaders.Remove(receipt);
            }

            if (receipts.Count > 0)
            {
                context.SaveChanges();
            }

            return receipts.Count;
        }

        /// <summary>Determines whether a receipt matches the sample payment created by the demo seeder.</summary>
        /// <param name="receipt">The receipt to inspect, with its lines loaded.</param>
        /// <returns><c>true</c> when the receipt is seeded sample data.</returns>
        public static bool IsSampleReceipt(SalesReceiptHeader receipt)
        {
            ArgumentNullException.ThrowIfNull(receipt);

            if (receipt.Amount != SampleAmount)
            {
                return false;
            }

            // Original seed: no payment details and lines that credit nothing specific (the UI always sets these).
            var isLegacySeed = receipt.PaymentMethod == null
                && receipt.Memo == null
                && receipt.SalesReceiptLines.Count > 0
                && receipt.SalesReceiptLines.All(l => l.AccountToCreditId == null && l.SalesInvoiceLineId == null && l.ItemId == null);

            // Later seed: cheque with a generated reference derived from the receipt date.
            var isGeneratedSeed = receipt.PaymentMethod == SamplePaymentMethod
                && receipt.ReferenceNo == $"CHQ-{receipt.Date:MMdd}"
                && receipt.Memo != null
                && receipt.Memo.StartsWith(SampleMemoPrefix, StringComparison.Ordinal);

            return isLegacySeed || isGeneratedSeed;
        }

        private static void RemoveAllocationLedgerEntry(ApiDbContext context, CustomerAllocation allocation)
        {
            var invoice = context.SalesInvoiceHeaders
                .Include(i => i.GeneralLedgerHeader!).ThenInclude(g => g.GeneralLedgerLines)
                .FirstOrDefault(i => i.Id == allocation.SalesInvoiceHeaderId);

            var linkedEntry = invoice?.GeneralLedgerHeader;
            if (linkedEntry != null && IsAllocationEntry(linkedEntry, allocation))
            {
                // Saving an allocation re-points the invoice at the allocation entry; restore the invoice's own entry.
                invoice!.GeneralLedgerHeader = context.GeneralLedgerHeaders
                    .Where(g => g.DocumentType == DocumentTypes.SalesInvoice
                        && g.Date == invoice.Date
                        && !context.SalesInvoiceHeaders.Any(i => i.GeneralLedgerHeaderId == g.Id))
                    .OrderBy(g => g.Id)
                    .FirstOrDefault();
                invoice.GeneralLedgerHeaderId = invoice.GeneralLedgerHeader?.Id;
                RemoveLedgerHeader(context, linkedEntry);
                return;
            }

            var orphanEntry = context.GeneralLedgerHeaders
                .Include(g => g.GeneralLedgerLines)
                .Where(g => g.DocumentType == DocumentTypes.CustomerAllocation
                    && g.Date == allocation.Date
                    && !context.SalesInvoiceHeaders.Any(i => i.GeneralLedgerHeaderId == g.Id))
                .AsEnumerable()
                .FirstOrDefault(g => IsAllocationEntry(g, allocation));
            RemoveLedgerHeader(context, orphanEntry);
        }

        private static bool IsAllocationEntry(Core.Domain.Financials.GeneralLedgerHeader entry, CustomerAllocation allocation) =>
            entry.DocumentType == DocumentTypes.CustomerAllocation
            && entry.Date == allocation.Date
            && entry.GeneralLedgerLines.Any(l => l.DrCr == DrOrCrSide.Cr && l.Amount == allocation.Amount);

        private static void RemoveLedgerHeader(ApiDbContext context, Core.Domain.Financials.GeneralLedgerHeader? entry)
        {
            if (entry == null)
            {
                return;
            }

            context.GeneralLedgerLines.RemoveRange(entry.GeneralLedgerLines);
            context.GeneralLedgerHeaders.Remove(entry);
        }
    }
}
