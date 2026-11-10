using System;

namespace Dto.Sales
{
    /// <summary>
    /// Summarizes a sales invoice that still has an unpaid balance.
    /// </summary>
    public class OutstandingInvoice
    {
        /// <summary>Gets or sets the invoice identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the invoice number.</summary>
        public string? No { get; set; }

        /// <summary>Gets or sets the customer identifier.</summary>
        public int CustomerId { get; set; }

        /// <summary>Gets or sets the customer name.</summary>
        public string? CustomerName { get; set; }

        /// <summary>Gets or sets the invoice date.</summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>Gets or sets the discounted item subtotal.</summary>
        public decimal Subtotal { get; set; }

        /// <summary>Gets or sets the shipping and handling charge.</summary>
        public decimal Shipping { get; set; }

        /// <summary>Gets or sets the tax added to the subtotal and shipping.</summary>
        public decimal Tax { get; set; }

        /// <summary>Gets the invoice total including shipping and tax.</summary>
        public decimal Total => Subtotal + Shipping + Tax;

        /// <summary>Gets or sets the amount already allocated from customer payments.</summary>
        public decimal Paid { get; set; }

        /// <summary>Gets the unpaid invoice balance.</summary>
        public decimal Balance => Total - Paid;

        /// <summary>Gets or sets a value indicating whether the invoice is posted to the general ledger.</summary>
        public bool Posted { get; set; }
    }
}
