using System.Collections.Generic;

namespace Dto.Sales
{
    /// <summary>Contains the calculated invoice tax and configured customer tax rates.</summary>
    public class InvoiceTaxResult
    {
        /// <summary>Gets or sets the tax added to the invoice subtotal and shipping.</summary>
        public decimal Amount { get; set; }

        /// <summary>Gets or sets the percentages from the customer's active tax group.</summary>
        public IList<decimal> Rates { get; set; } = new List<decimal>();
    }

    /// <summary>Represents invoice lines used to preview customer-based sales tax.</summary>
    public class InvoiceTaxRequest
    {
        /// <summary>Gets or sets the customer whose tax group applies.</summary>
        public int CustomerId { get; set; }

        /// <summary>Gets or sets the taxable shipping charge.</summary>
        public decimal Shipping { get; set; }

        /// <summary>Gets or sets the invoice lines to evaluate.</summary>
        public IList<InvoiceTaxLine> Lines { get; set; } = new List<InvoiceTaxLine>();
    }

    /// <summary>Represents one invoice line used for a tax preview.</summary>
    public class InvoiceTaxLine
    {
        /// <summary>Gets or sets the item identifier.</summary>
        public int ItemId { get; set; }

        /// <summary>Gets or sets the quantity.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Gets or sets the price per unit.</summary>
        public decimal Amount { get; set; }

        /// <summary>Gets or sets the discount percentage.</summary>
        public decimal Discount { get; set; }
    }
}
