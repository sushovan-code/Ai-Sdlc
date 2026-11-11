using FluentValidation;

namespace AiSdlc.Application.Stages.GenerateStage;

public sealed class GenerateStageValidator : AbstractValidator<GenerateStage>
{
    public GenerateStageValidator() => RuleFor(x => x.Feedback).MaximumLength(Analysis.AiLimits.MaxFeedbackLength);
}
