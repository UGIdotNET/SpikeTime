namespace UGIdotNET.SpikeTime.AkkaNet.App.Devices;

public class Device : UntypedActor
{
    private double? _lastTemperatureReading = null;

    protected ILoggingAdapter Log { get; } = Context.GetLogger();
    protected string GroupId { get; }
    protected string DeviceId { get; }

    public Device(string groupId, string deviceId)
    {
        GroupId = groupId;
        DeviceId = deviceId;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case RequestTrackDevice req when req.GroupId.Equals(GroupId) && req.DeviceId.Equals(DeviceId):
                Sender.Tell(new DeviceRegistered(DeviceId));
                break;
            case RequestTrackDevice req:
                Log.Warning($"Ignoring TrackDevice request for {req.GroupId}-{req.DeviceId}.This actor is responsible for {GroupId}-{DeviceId}.");
                break;
            case RecordTemperature rec:
                Log.Info($"Recorded temperature reading {rec.Value} with {rec.RequestId}");
                _lastTemperatureReading = rec.Value;
                Sender.Tell(new TemperatureRecorded(rec.RequestId));
                break;
            case ReadTemperature read:
                Sender.Tell(new RespondTemperature(read.RequestId, _lastTemperatureReading));
                break;
        }
    }

    public static Props Props(string groupId, string deviceId) =>
       Akka.Actor.Props.Create(() => new Device(groupId, deviceId));
}
