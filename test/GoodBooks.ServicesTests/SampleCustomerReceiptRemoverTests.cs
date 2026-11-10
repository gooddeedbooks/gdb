using Api.Data;
using Api.Data.Seed;
using Core.Domain;
using Core.Domain.Financials;
using Core.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GoodBooks.ServicesTests
{
    public class SampleCustomerReceiptRemoverTests
    {
        private static readonly DateTime InvoiceDate = new(2026, 2, 5);
        private static readonly DateTime ReceiptDate = new(2026, 2, 19);

        private readonly Mock<ApiDbContext> context = new(new DbContextOptionsBuilder<ApiDbContext>().Options);
        private readonly Mock<DbSet<SalesReceiptHeader>> receiptSet = new();
        private readonly Mock<DbSet<SalesReceiptLine>> receiptLineSet = new();
        private readonly Mock<DbSet<CustomerAllocation>> allocationSet = new();
        private readonly Mock<DbSet<GeneralLedgerHeader>> ledgerSet = new();
        private readonly Mock<DbSet<GeneralLedgerLine>> ledgerLineSet = new();
        private readonly Mock<DbSet<SalesInvoiceHeader>> invoiceSet = new();

        [Fact]
        public void RemoveSampleReceipts_SeededAllocatedPayment_RemovesPaymentAndRestoresInvoiceLedgerEntry()
        {
            var receipt = SeededReceipt();
            var invoiceEntry = Ledger(1, DocumentTypes.SalesInvoice, InvoiceDate, 678m);
            var allocationEntry = Ledger(2, DocumentTypes.CustomerAllocation, ReceiptDate, 300m);
            var invoice = new SalesInvoiceHeader { Id = 21, Date = InvoiceDate, GeneralLedgerHeaderId = 2, GeneralLedgerHeader = allocationEntry };
            var allocation = new CustomerAllocation { Id = 5, SalesReceiptHeaderId = receipt.Id, SalesInvoiceHeaderId = invoice.Id, Date = ReceiptDate, Amount = 300m };
            Setup([receipt], [invoice], [allocation], [invoiceEntry, allocationEntry, receipt.GeneralLedgerHeader!]);

            var removed = SampleCustomerReceiptRemover.RemoveSampleReceipts(context.Object);

            Assert.Equal(1, removed);
            Assert.Same(invoiceEntry, invoice.GeneralLedgerHeader);
            Assert.Equal(invoiceEntry.Id, invoice.GeneralLedgerHeaderId);
            receiptSet.Verify(s => s.Remove(receipt), Times.Once);
            allocationSet.Verify(s => s.RemoveRange(It.Is<IEnumerable<CustomerAllocation>>(a => a.Single() == allocation)), Times.Once);
            ledgerSet.Verify(s => s.Remove(allocationEntry), Times.Once);
            ledgerSet.Verify(s => s.Remove(receipt.GeneralLedgerHeader!), Times.Once);
            ledgerSet.Verify(s => s.Remove(invoiceEntry), Times.Never);
            context.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void RemoveSampleReceipts_UserEnteredPayment_IsKept()
        {
            var receipt = SeededReceipt();
            receipt.ReferenceNo = "CHQ-55521";
            Setup([receipt], [], [], []);

            var removed = SampleCustomerReceiptRemover.RemoveSampleReceipts(context.Object);

            Assert.Equal(0, removed);
            receiptSet.Verify(s => s.Remove(It.IsAny<SalesReceiptHeader>()), Times.Never);
            context.Verify(c => c.SaveChanges(), Times.Never);
        }

        [Fact]
        public void IsSampleReceipt_OriginalSeedWithoutPaymentDetails_ReturnsTrue()
        {
            var receipt = new SalesReceiptHeader { Amount = 300m, Date = ReceiptDate };
            receipt.SalesReceiptLines.Add(new SalesReceiptLine { Amount = 300m });

            Assert.True(SampleCustomerReceiptRemover.IsSampleReceipt(receipt));
        }

        [Fact]
        public void IsSampleReceipt_PaymentWithCreditAccount_ReturnsFalse()
        {
            var receipt = new SalesReceiptHeader { Amount = 300m, Date = ReceiptDate };
            receipt.SalesReceiptLines.Add(new SalesReceiptLine { Amount = 300m, AccountToCreditId = 2 });

            Assert.False(SampleCustomerReceiptRemover.IsSampleReceipt(receipt));
        }

        private static SalesReceiptHeader SeededReceipt()
        {
            var receipt = new SalesReceiptHeader
            {
                Id = 11,
                Date = ReceiptDate,
                Amount = 300m,
                PaymentMethod = "Cheque",
                ReferenceNo = "CHQ-0219",
                Memo = "Payment for invoice #1",
                GeneralLedgerHeader = Ledger(3, DocumentTypes.SalesReceipt, ReceiptDate, 300m)
            };
            receipt.SalesReceiptLines.Add(new SalesReceiptLine { Amount = 300m, AccountToCreditId = 2 });
            return receipt;
        }

        private static GeneralLedgerHeader Ledger(int id, DocumentTypes type, DateTime date, decimal amount)
        {
            var header = new GeneralLedgerHeader { Id = id, DocumentType = type, Date = date };
            header.GeneralLedgerLines.Add(new GeneralLedgerLine { DrCr = DrOrCrSide.Dr, Amount = amount });
            header.GeneralLedgerLines.Add(new GeneralLedgerLine { DrCr = DrOrCrSide.Cr, Amount = amount });
            return header;
        }

        private void Setup(
            List<SalesReceiptHeader> receipts,
            List<SalesInvoiceHeader> invoices,
            List<CustomerAllocation> allocations,
            List<GeneralLedgerHeader> ledgerEntries)
        {
            context.Setup(c => c.SalesReceiptHeaders).Returns(Bind(receiptSet, receipts));
            context.Setup(c => c.SalesReceiptLines).Returns(Bind(receiptLineSet, []));
            context.Setup(c => c.CustomerAllocations).Returns(Bind(allocationSet, allocations));
            context.Setup(c => c.GeneralLedgerHeaders).Returns(Bind(ledgerSet, ledgerEntries));
            context.Setup(c => c.GeneralLedgerLines).Returns(Bind(ledgerLineSet, []));
            context.Setup(c => c.SalesInvoiceHeaders).Returns(Bind(invoiceSet, invoices));
        }

        private static DbSet<T> Bind<T>(Mock<DbSet<T>> set, List<T> values) where T : BaseEntity
        {
            var query = values.AsQueryable();
            set.As<IQueryable<T>>().Setup(s => s.Provider).Returns(query.Provider);
            set.As<IQueryable<T>>().Setup(s => s.Expression).Returns(query.Expression);
            set.As<IQueryable<T>>().Setup(s => s.ElementType).Returns(query.ElementType);
            set.As<IQueryable<T>>().Setup(s => s.GetEnumerator()).Returns(() => values.GetEnumerator());
            return set.Object;
        }
    }
}
