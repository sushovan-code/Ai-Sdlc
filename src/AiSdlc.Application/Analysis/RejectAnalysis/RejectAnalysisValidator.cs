using FluentValidation;

namespace AiSdlc.Application.Analysis.RejectAnalysis;

public sealed class RejectAnalysisValidator : AbstractValidator<RejectAnalysis>
{
    public RejectAnalysisValidator() => RuleFor(x => x.Comment).NotEmpty().MaximumLength(AiLimits.MaxFeedbackLength);
}
