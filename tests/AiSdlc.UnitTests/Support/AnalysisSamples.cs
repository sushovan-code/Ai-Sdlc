namespace AiSdlc.UnitTests.Support;

public static class AnalysisSamples
{
    public const string CriticJson = """{"notes":[{"severity":"High","note":"Refund rules are missing."}]}""";

    public const string DraftJson = """
        {"summary":"Pay with a saved card.",
         "functionalRequirements":[{"id":"FR-1","title":"Pay","description":"Pay with a card.","priority":"high","sourceRefs":["C1"]}],
         "nonFunctionalRequirements":[],"ambiguities":[],"gaps":[],"assumptions":[],"risks":[]}
        """;
}
