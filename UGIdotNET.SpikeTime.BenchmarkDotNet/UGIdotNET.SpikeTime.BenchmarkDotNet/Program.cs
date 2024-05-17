// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using UGIdotNET.SpikeTime.BenchmarkDotNet.Lib;

Console.WriteLine("Spike Time - Benchmark.NET");

//RunMd5VsSha256();
//RunIntroArguments();
//RunIntroExportJson();
//RunIntroExportJsonWithConfigClass();
//RunFluentConfiguration();
RunGenericTypeArguments(args);

void RunGenericTypeArguments(string[] args)
{
    BenchmarkSwitcher.FromTypes([typeof(IntroGenericTypeArguments<>)]).Run(args);
}

void RunFluentConfiguration()
{
    BenchmarkRunner.Run<Algo_Md5VsSha256>(
        DefaultConfig.Instance
            .AddJob(Job.Default.WithRuntime(ClrRuntime.Net48))
            .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)));
}

void RunIntroExportJson()
{
    BenchmarkRunner.Run<IntroExportJson>();
}

void RunIntroExportJsonWithConfigClass()
{
    BenchmarkRunner.Run<IntroJsonExportObjectStyle>();
}

void RunMd5VsSha256()
{
    BenchmarkRunner.Run<Md5VsSha256>();
}

void RunIntroArguments()
{
    BenchmarkRunner.Run<IntroArguments>();
}