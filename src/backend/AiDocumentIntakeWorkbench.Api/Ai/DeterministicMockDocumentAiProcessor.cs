using AiDocumentIntakeWorkbench.Api.SampleDocuments;

namespace AiDocumentIntakeWorkbench.Api.Ai;

public sealed class DeterministicMockDocumentAiProcessor : IDocumentAiProcessor
{
    public Task<DocumentAiProcessingResult> ProcessAsync(
        DocumentAiInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.SampleDocumentId))
        {
            return Task.FromResult(DocumentAiProcessingResult.Failure(
                DocumentAiProcessingError.MissingSampleDocumentId));
        }

        var result = input.SampleDocumentId.Trim().ToLowerInvariant() switch
        {
            SampleDocumentIds.CleanHighConfidence => CreateCleanHighConfidenceOutput(),
            SampleDocumentIds.MissingLowConfidence => CreateMissingLowConfidenceOutput(),
            SampleDocumentIds.ConflictingInconsistent => CreateConflictingInconsistentOutput(),
            _ => DocumentAiProcessingResult.Failure(DocumentAiProcessingError.UnsupportedSampleDocument)
        };

        return Task.FromResult(result);
    }

    private static DocumentAiProcessingResult CreateCleanHighConfidenceOutput()
    {
        return DocumentAiProcessingResult.Success(new DocumentAiOutput(
            SampleDocumentIds.CleanHighConfidence,
            "VendorInvoice",
            0.97m,
            [
                new("Supplier", "Northwind Office Supply", 0.98m),
                new("InvoiceNumber", "INV-10482", 0.99m),
                new("InvoiceDate", "2026-06-12", 0.96m),
                new("DueDate", "2026-07-12", 0.96m),
                new("TotalDue", "$4,218.40", 0.98m),
                new("PaymentInstructions", "ACH to Northwind Office Supply, reference INV-10482", 0.95m)
            ],
            [],
            [],
            "ReadyForStandardHumanReview",
            "The invoice contains the expected supplier, invoice number, amount, due date, and payment details."));
    }

    private static DocumentAiProcessingResult CreateMissingLowConfidenceOutput()
    {
        return DocumentAiProcessingResult.Success(new DocumentAiOutput(
            SampleDocumentIds.MissingLowConfidence,
            "FacilitiesServiceRequest",
            0.58m,
            [
                new("RequestedBy", "Regional Office Team", 0.82m),
                new("ServiceNeeded", "Replace damaged lobby access badge reader", 0.78m),
                new("PreferredDate", "2026-07-18", 0.74m),
                new("Notes", "Reader works intermittently and blocks employee entry during peak hours", 0.70m)
            ],
            [
                "AccountReference"
            ],
            [
                "Customer account reference is not provided.",
                "Several fields require human confirmation before downstream workflow use."
            ],
            "HumanReviewRequired",
            "The document includes useful request details, but the missing account reference lowers confidence."));
    }

    private static DocumentAiProcessingResult CreateConflictingInconsistentOutput()
    {
        return DocumentAiProcessingResult.Success(new DocumentAiOutput(
            SampleDocumentIds.ConflictingInconsistent,
            "PurchaseOrder",
            0.69m,
            [
                new("PurchaseOrderNumber", "PO-77831", 0.96m),
                new("Buyer", "Fabrikam Retail Group", 0.91m),
                new("Supplier", "Alpine Equipment Parts", 0.91m),
                new("StatedSubtotal", "$1,450.00", 0.88m),
                new("StatedOrderTotal", "$1,900.00", 0.88m),
                new("CalculatedLineTotal", "$1,450.00", 0.86m),
                new("RequestedDeliveryDate", "2026-08-02", 0.90m)
            ],
            [],
            [
                "Stated order total conflicts with the line total and stated subtotal.",
                "Purchase order totals should be checked by a human reviewer before any final workflow action."
            ],
            "HumanReviewRequired",
            "The purchase order has recognizable structure, but the totals are inconsistent."));
    }
}
