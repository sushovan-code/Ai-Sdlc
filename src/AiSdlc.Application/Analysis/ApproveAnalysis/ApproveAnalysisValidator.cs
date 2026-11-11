using FluentValidation;

namespace AiSdlc.Application.Analysis.ApproveAnalysis;

public sealed class ApproveAnalysisValidator : AbstractValidator<ApproveAnalysis>
{
    public ApproveAnalysisValidator() => RuleFor(x => x.Comment).MaximumLength(AiLimits.MaxFeedbackLength);
}
