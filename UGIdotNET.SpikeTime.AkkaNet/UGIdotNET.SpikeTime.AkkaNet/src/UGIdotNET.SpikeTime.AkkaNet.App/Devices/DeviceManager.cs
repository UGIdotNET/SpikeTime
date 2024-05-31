namespace UGIdotNET.SpikeTime.AkkaNet.App.Devices;

public class DeviceManager : UntypedActor
{
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case CreateDeviceGroup createDeviceGroup:
                var deviceId = new Random().Next(10);

                var deviceGroup = Context.ActorOf(DeviceGroup.Props(createDeviceGroup.GroupId));
                deviceGroup.Forward(new RequestTrackDevice(createDeviceGroup.GroupId, $"device{deviceId}"));
                break;
            default:
                break;
        }
    }

    public static Props Props() => Akka.Actor.Props.Create(() => new DeviceManager());
}
