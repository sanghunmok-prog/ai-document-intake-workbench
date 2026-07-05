namespace AiDocumentIntakeWorkbench.Api.SampleDocuments;

public sealed class InMemorySampleDocumentCatalog : ISampleDocumentCatalog
{
    private static readonly IReadOnlyList<SampleDocument> Samples =
    [
        new(
            "clean-high-confidence",
            "Vendor invoice with complete remittance details",
            "Complete invoice scenario",
            "A vendor invoice includes a clear supplier, invoice number, total, due date, and payment instructions.",
            """
            Northwind Office Supply
            Invoice INV-10482
            Bill to: Contoso Operations
            Invoice date: 2026-06-12
            Due date: 2026-07-12
            Total due: $4,218.40
            Payment instructions: ACH to Northwind Office Supply, reference INV-10482.
            """),
        new(
            "missing-low-confidence",
            "Service request with missing account reference",
            "Incomplete service request scenario",
            "A service request includes useful business context but omits the customer account reference.",
            """
            Facilities Service Request
            Requested by: Regional Office Team
            Service needed: Replace damaged lobby access badge reader.
            Preferred date: 2026-07-18
            Account reference: not provided
            Notes: The reader works intermittently and blocks employee entry during peak hours.
            """),
        new(
            "conflicting-inconsistent",
            "Purchase order with inconsistent totals",
            "Conflicting purchase order scenario",
            "A purchase order includes line items whose subtotal does not match the stated order total.",
            """
            Purchase Order PO-77831
            Buyer: Fabrikam Retail Group
            Supplier: Alpine Equipment Parts
            Line 1: Replacement scanner docks, quantity 4, unit price $300.00
            Line 2: Charging cables, quantity 10, unit price $25.00
            Stated subtotal: $1,450.00
            Stated order total: $1,900.00
            Requested delivery date: 2026-08-02
            """)
    ];

    public IReadOnlyList<SampleDocument> GetAll()
    {
        return Samples;
    }

    public SampleDocument? FindById(string sampleDocumentId)
    {
        return Samples.FirstOrDefault(sample =>
            string.Equals(sample.Id, sampleDocumentId, StringComparison.OrdinalIgnoreCase));
    }
}
