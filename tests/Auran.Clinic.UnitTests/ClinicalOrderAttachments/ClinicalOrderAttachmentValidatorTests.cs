using Auran.Clinic.Application.ClinicalOrderAttachments;

namespace Auran.Clinic.UnitTests.ClinicalOrderAttachments;

public sealed class ClinicalOrderAttachmentValidatorTests
{
    [Fact]
    public void Link_requires_visit_file_and_section()
    {
        var validator = new LinkClinicalOrderAttachmentRequestValidator();

        var result = validator.Validate(new LinkClinicalOrderAttachmentRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Link_accepts_complete_identifiers()
    {
        var validator = new LinkClinicalOrderAttachmentRequestValidator();

        var result = validator.Validate(new LinkClinicalOrderAttachmentRequest
        {
            VisitId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            SectionDefinitionId = Guid.NewGuid()
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Delete_requires_link_id()
    {
        var validator = new DeleteClinicalOrderAttachmentRequestValidator();

        var result = validator.Validate(new DeleteClinicalOrderAttachmentRequest());

        Assert.False(result.IsValid);
    }
}
