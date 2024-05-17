using BenchmarkDotNet.Attributes;
using System;

namespace UGIdotNET.SpikeTime.BenchmarkDotNet.Lib
{
    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(char))]
    public class IntroGenericTypeArguments<T>
    {
        [Benchmark] public T Create() => Activator.CreateInstance<T>();
    }
}
