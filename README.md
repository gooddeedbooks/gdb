# GoodDeedBooks

GoodDeedBooks is an open-source business accounting application with a focus on nonprofit donation management. It builds on the AccountGo accounting codebase and combines core bookkeeping workflows with donor and donation tracking.

## What the application does

- Supports accounting workflows such as accounts receivable, accounts payable, inventory, and financial management.
- Seeds five sample open sales orders using available active customers, priced items, and payment terms; seeding is repeatable and does not duplicate those orders.
- Edits existing sales orders from **Accounts Receivable > Sales Orders**: select an order and choose **Edit** to change its customer, payment term, and line items, then save the original order. Lines already used by invoices cannot be removed.
- Creates invoices from selected sales orders using **Create Invoice**, prefilling the customer, payment terms, and remaining uninvoiced quantities with their prices, discounts, and units.
- Keeps all invoicing in **Accounts Receivable > Sales Orders**. The **Outstanding Invoices** tab lists unpaid invoices for all customers with subtotal, shipping, tax, total, paid, and balance amounts, a customer filter, and totals. Use **New Invoice** to add a standalone invoice, or select a draft invoice and choose **Edit Invoice** (or double-click it); posted invoices are read-only.
- Splits **Accounts Receivable > Customer Payments** into **Outstanding Invoices** and **Payments Received** tabs; both can be filtered by customer. On Outstanding Invoices, select an invoice and choose **Receive Payment** (or double-click it) to open a new payment prefilled with the customer, the invoice balance, and a memo. Saving applies the payment to that invoice, up to its balance; any excess stays on the receipt for later allocation.
- Suggests the next numeric invoice number (starting at 1 for an empty invoice ledger), allows manual overrides, and rejects duplicate numbers. Invoice rows can be deleted before saving and show a read-only line amount. Sales invoice prices exclude tax: customer-group taxes apply to the discounted item subtotal plus shipping and are added to **Total Invoice**. The tax label shows the percentages (for example, **Tax 5% + 7%**), and changing shipping recalculates tax. Preview and posting use the same calculation.
- Manages shared units of measure under **Inventory Control > Units of Measure**, with code and description lookup values used by item and transaction dropdowns. Units already in use cannot be deleted.
- Manages donors and donation types, records donation transactions and payment details, and associates donations with general-ledger accounts.
- Supports Canadian tax-receipt workflows, including receipt generation and PDF output.

The application is evolving. Some parts of the repository retain the AccountGo name and older front-end implementations; features and setup instructions in this README describe the current GoodDeedBooks development setup.

Startup completes the **ABC Customer** and **Northwind Books** samples with fictional phone/fax numbers, `example.com` email and website addresses, company/shipping addresses, primary contacts, payment terms, and invoicing accounts. Missing tax assignments follow the sample's company province. Repeated seeding fills blank fields only and preserves populated values and other customers.

### Canadian sales taxes

**Tax > Taxes** manages individual tax components, province/territory tax groups, and item tax groups. Startup adds missing Canadian defaults without replacing existing rates, GL mappings, groups, or customer assignments. Tax percentages support three decimal places, including Quebec's 9.975% QST.

| Province / territory | Standard components | Combined rate |
| --- | --- | --- |
| Alberta, Northwest Territories, Nunavut, Yukon | GST 5% | 5% |
| British Columbia | GST 5% + PST 7% | 12% |
| Manitoba | GST 5% + RST 7% | 12% |
| Saskatchewan | GST 5% + PST 6% | 11% |
| Quebec | GST 5% + QST 9.975% | 14.975% |
| Ontario | HST 13% | 13% |
| Nova Scotia | HST 14% | 14% |
| New Brunswick, Newfoundland and Labrador, Prince Edward Island | HST 15% | 15% |

Select the customer's `CA-XX - Province` tax group according to the **place of supply**, not necessarily their billing address. HST is one tax, not GST plus HST. Sales invoices use **tax-exclusive** prices: each customer-group tax applies to the discounted item subtotal plus shipping, without compounding. Each tax component is rounded to cents before summing, consistently for preview and posting. For example, $350 of items plus $15 shipping at 14% produces $51.10 tax and a $416.10 invoice total. Purchase tax extraction retains its tax-inclusive behavior. The group table shows individual components and the combined percentage; invoice labels retain all three QST decimals. Previously posted ledger entries are not rewritten.

The customer form lists all tax groups (inactive groups are marked). For customers without an assigned group, the default follows **Company Address > Province**; changing that province updates the automatic default until a tax group is manually selected. Existing assignments and manual selections are preserved and can be overridden for a different place of supply.

Legacy tax groups remain available for review; existing customers are **not** reassigned automatically. Newly created demo customers use Ontario's group. Tax edits preserve memberships in all provincial groups that share that component. Separate zero-rated and exempt customer groups contain no tax components.

These are standard current rates (Nova Scotia's 14% applies from April 1, 2025), not an automated historical-rate, product-exemption, rebate, or place-of-supply rules engine. Review older transactions, special supplies, provincial shipping treatment, and purchase-tax recoverability separately. Default GL accounts remain 20300 for sales tax and 50700 for purchase tax; review these mappings with your accountant. The database migration and additive seeding run when the API starts.

Sources: [Retail Council of Canada provincial table](https://www.retailcouncil.org/resources/quick-facts/sales-tax-rates-by-province/) and [CRA GST/HST and provincial rates](https://www.canada.ca/en/revenue-agency/services/tax/businesses/topics/gst-hst-businesses/charge-collect-which-rate/calculator.html).

## Application structure

- **`src/Api`** — ASP.NET Core API and data services.
- **`src/AccountGoWeb`** — the primary web application, built with ASP.NET Core and Razor components.
- **`src/Core`, `src/Services`, `src/Dto`, and `src/Infrastructure`** — domain models, business services, API contracts, and persistence support.
- **`src/MigrationService`** — applies database migrations during local Aspire orchestration.
- **`src/src.AppHost`** — .NET Aspire entry point that starts SQL Server, the API, the web application, and the migration service.
- **`src/BlazorGDB`** — an additional Blazor application project; it is not the web application launched by the current Aspire AppHost.

The main applications target .NET 10 and use Entity Framework Core with SQL Server. The repository also contains older or experimental front-end projects.

## Run locally

Prerequisites:

- .NET 10 SDK
- Docker Desktop or another Docker-compatible runtime, for the SQL Server container managed by Aspire

From the repository root, start the application:

```sh
dotnet run --project src/src.AppHost
```

Aspire starts the database, applies migrations, and launches the API and web application. Use the Aspire dashboard links in the console output to open the services and inspect their status and logs. The AppHost currently configures the API on port `8001` and the web application on port `8000`.

To build the solution:

```sh
dotnet build src/src.sln
```

## Donations and database changes

Donation records are associated with donors and donation types and include payment, amount, general-ledger, and tax-receipt information. When a migration affects an existing database, back it up before applying the migration. Some historical donation data cannot be automatically converted back to its former representation; use a database backup to roll back such changes.

Database migrations are run by the Aspire `MigrationService` during local startup. For deployment or other environments, configure the API and migration service with the appropriate SQL Server connection string and follow the deployment environment's migration process.

## Repository

Source code and issue tracking: [gooddeedbooks/gdb](https://github.com/gooddeedbooks/gdb).
