namespace UGIdotNET.SpikeTime.AkkaNet.App.Devices;

public record ReadTemperature(long RequestId);

public record RespondTemperature(long RequestId, double? Value);

public record RecordTemperature(long RequestId, double Value);

public record TemperatureRecorded(long RequestId);

public record RequestTrackDevice(string GroupId, string DeviceId);

public record DeviceRegistered(string DeviceId);

public record CreateDeviceGroup(string GroupId);
