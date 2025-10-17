public static class PromptSuggestionsDataProvider
{
    public static List<PromptSuggestion> GetData()
    {
        List<PromptSuggestion> result =
        [
            new("Tell me a joke", "Take a break and enjoy a quick laugh", "Tell me a joke."),
            new("Summarize text", "Extract a quick summary (main ideas)", "Summarize the following text:"),
            new("Write an email", "Make your text look and sound professional", "Format text as a formal email to a client:"),
            new("Brainstorm ideas", "Get creative input for your tasks", "Help me brainstorm ideas for:"),
            new("Fix my writing", "Avoid spelling, grammar, and style errors", "Proofread the following text:"),
        ];
        return result;
    }
}
