using SmartComponents.Inference;
using SmartComponents.Infrastructure;
using SmartComponents.StaticAssets.Inference;

namespace UGIdotNET.SpikeTimeSmartComponents;

class MySmartTextAreaInference : SmartTextAreaInference
{
    public override ChatParameters BuildPrompt(
        SmartTextAreaConfig config, string textBefore, string textAfter)
    {
        var prompt = base.BuildPrompt(config, textBefore, textAfter);

        prompt.Messages!.Add(new(ChatMessageRole.System,
            "The suggestions must ALWAYS BE IN ALL CAPS."));

        prompt.Temperature = 0.5f; // Less deterministic, more creative (default = 0)

        return prompt;
    }
}
