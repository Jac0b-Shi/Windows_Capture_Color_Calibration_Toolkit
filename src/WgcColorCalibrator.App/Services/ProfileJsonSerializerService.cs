using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Application-level wrapper for measurement serialization.
/// </summary>
public sealed class ProfileJsonSerializerService
{
    public string SerializeMeasurement(MeasurementSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return ProfileJsonSerializer.Serialize(session);
    }
}
