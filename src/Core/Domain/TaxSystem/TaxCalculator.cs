using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Domain.TaxSystem
{
    /// <summary>Calculates non-compounding taxes on a shared taxable base.</summary>
    public static class TaxCalculator
    {
        /// <summary>Calculates tax added to a tax-exclusive amount.</summary>
        /// <param name="amount">The discounted subtotal plus taxable shipping.</param>
        /// <param name="taxes">The applicable customer taxes.</param>
        /// <returns>Tax amounts rounded per component for display and posting.</returns>
        public static List<KeyValuePair<int, decimal>> AddedTaxes(decimal amount, IEnumerable<Tax> taxes)
        {
            if (taxes == null)
                throw new ArgumentNullException(nameof(taxes));
            return taxes.Select(t => new KeyValuePair<int, decimal>(t.Id,
                Math.Round(amount * t.Rate / 100, 2, MidpointRounding.ToEven))).ToList();
        }

        /// <summary>Splits included taxes using the same pre-tax base for each component.</summary>
        /// <param name="quantity">The line quantity.</param>
        /// <param name="price">The tax-inclusive unit price.</param>
        /// <param name="discount">The percentage discount.</param>
        /// <param name="taxes">The applicable GST, HST, PST, RST, or QST components.</param>
        /// <returns>Unrounded tax amounts keyed by tax identifier.</returns>
        public static List<KeyValuePair<int, decimal>> IncludedTaxes(
            decimal quantity, decimal price, decimal discount, IEnumerable<Tax> taxes)
        {
            if (taxes == null)
                throw new ArgumentNullException(nameof(taxes));

            var components = taxes.ToList();
            var gross = quantity * price * (1 - discount / 100);
            var net = gross / (1 + components.Sum(t => t.Rate) / 100);
            return components.Select(t => new KeyValuePair<int, decimal>(t.Id, net * t.Rate / 100)).ToList();
        }
    }
}
