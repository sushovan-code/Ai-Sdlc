using FluentValidation;

namespace AiSdlc.Application.Analysis.GenerateAnalysis;

public sealed class GenerateAnalysisValidator : AbstractValidator<GenerateAnalysis>
{
    public GenerateAnalysisValidator() => RuleFor(x => x.Feedback).MaximumLength(AiLimits.MaxFeedbackLength);
}
