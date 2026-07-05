namespace AiDocumentIntakeWorkbench.Api.SampleDocuments;

public interface ISampleDocumentCatalog
{
    IReadOnlyList<SampleDocument> GetAll();

    SampleDocument? FindById(string sampleDocumentId);
}
