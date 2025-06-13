using Akka.Hosting;
using Microsoft.Extensions.Hosting;
using UGIdotNET.SpikeTime.AkkaNet.App;
using UGIdotNET.SpikeTime.AkkaNet.App.Devices;

var hostBuilder = new HostBuilder();

hostBuilder.ConfigureServices((context, services) =>
{
	services.AddAkka("SpikeTimeActorSystem", (builder, sp) =>
	{
		builder
			.WithActors((system, registry, resolver) =>
			{
				var helloActor = system.ActorOf(Props.Create(() => new HelloActor()), "hello-actor");
				registry.Register<HelloActor>(helloActor);
			})
			.WithActors((system, registry, resolver) =>
			{
				var timerActorProps =
					resolver.Props<TimerActor>(); // uses Msft.Ext.DI to inject reference to helloActor
				var timerActor = system.ActorOf(timerActorProps, "timer-actor");
				registry.Register<TimerActor>(timerActor);
			})
			.WithActors((system, registry, resolver) =>
			{
				var deviceActor = system.ActorOf(Device.Props("groupId", "deviceId"), "device-actor");
				registry.Register<Device>(deviceActor);
			})
			.WithActors((system, registry, resolver) =>
			{
				var deviceGroupActor = system.ActorOf(DeviceGroup.Props("groupId"), "devicegroup-actor");
				registry.Register<DeviceGroup>(deviceGroupActor);
			})
			.WithActors((system, registry, resolver) =>
			{
				var deviceManagerActor = system.ActorOf(DeviceManager.Props(), "devicemanager-actor");
				registry.Register<DeviceManager>(deviceManagerActor);
			});
	});
});

var host = hostBuilder.Build();

await host.RunAsync();