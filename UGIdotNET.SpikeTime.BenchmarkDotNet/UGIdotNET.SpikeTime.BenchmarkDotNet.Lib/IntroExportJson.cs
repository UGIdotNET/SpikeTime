﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using System.Threading;

namespace UGIdotNET.SpikeTime.BenchmarkDotNet.Lib
{
    [DryJob]
    [JsonExporterAttribute.Brief]
    [JsonExporterAttribute.Full]
    [JsonExporterAttribute.BriefCompressed]
    [JsonExporterAttribute.FullCompressed]
    [JsonExporter("-custom", indentJson: true, excludeMeasurements: false)]
    public class IntroExportJson
    {
        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }

    // *** Object style ***

    [Config(typeof(Config))]
    public class IntroJsonExportObjectStyle
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(JsonExporter.Brief);
                AddExporter(JsonExporter.Brief);
                AddExporter(JsonExporter.Full);
                AddExporter(JsonExporter.BriefCompressed);
                AddExporter(JsonExporter.FullCompressed);
                AddExporter(JsonExporter.Custom("-custom", indentJson: true, excludeMeasurements: true));
            }
        }

        [Benchmark] public void Sleep10() => Thread.Sleep(10);
        [Benchmark] public void Sleep20() => Thread.Sleep(20);
    }
}
