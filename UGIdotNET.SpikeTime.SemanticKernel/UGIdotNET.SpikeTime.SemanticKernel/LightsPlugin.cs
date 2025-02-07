using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace UGIdotNET.SpikeTime.SemanticKernel;

public class LightsPlugin
{
    private readonly List<LightModel> _lights = [
        new() { Id = 1, Name = "Table Lamp", IsOn = false },
        new() { Id = 2, Name = "Porch light", IsOn = false },
        new() { Id = 3, Name = "Chandelier", IsOn = true }
    ];

    [KernelFunction("get_lights")]
    [Description("Ottiene l'elenco delle lampadina disponibili ed il loro stato corrente")]
    public async Task<List<LightModel>> GetLightsAsync()
    {
        await Task.Delay(1000);
        return _lights;
    }

    [KernelFunction("change_state")]
    [Description("Modifica lo stato della lampadina")]
    public async Task<LightModel?> ChangeStateAsync(int id, bool isOn)
    {
        var light = _lights.FirstOrDefault(x => x.Id == id);
        if (light is null)
        {
            return null;
        }

        light.IsOn = isOn;
        await Task.Delay(1000);

        return light;
    }

    public class LightModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsOn { get; set; }
    }
}