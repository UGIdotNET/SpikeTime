using Akka.Hosting;
using UGIdotNET.SpikeTime.AkkaNet.App.Devices;

namespace UGIdotNET.SpikeTime.AkkaNet.App;

public class TimerActor : ReceiveActor, IWithTimers
{
    private readonly IActorRef _deviceManagerActor;

    protected ILoggingAdapter Log { get; } = Context.GetLogger();

    public TimerActor(IRequiredActor<DeviceManager> deviceManagerActor)
    {
        _deviceManagerActor = deviceManagerActor.ActorRef;

        Receive<CreateDeviceGroup>(message =>
        {
            _deviceManagerActor.Tell(message);
        });

        Receive<DeviceRegistered>(message =>
        {
            Log.Info($"Device {message.DeviceId} registered correctly!");
        });
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer(
            "devicemanager-key", 
            new CreateDeviceGroup($"group{new Random().Next(10)}"), 
            TimeSpan.FromSeconds(10));
    }

    public ITimerScheduler Timers { get; set; } = null!; // gets set by Akka.NET
}