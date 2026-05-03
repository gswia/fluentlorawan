using NoReturn.LoRaWAN.Models;

namespace NoReturn.LoRaWAN;

/// <summary>
/// Provider interface for LoRaWAN device data persistence
/// </summary>
public interface ILoRaWANDataProvider
{
    /// <summary>
    /// Get device configuration including account, group, and sensor mappings
    /// </summary>
    /// <param name="deviceId">Device EUI (DevEUI)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Device configuration or null if device not found</returns>
    Task<DeviceConfiguration?> GetDeviceConfigurationAsync(
        string deviceId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Insert readings for a device
    /// </summary>
    /// <param name="deviceConfig">Device configuration with account/group IDs</param>
    /// <param name="readings">List of readings to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InsertReadingsAsync(
        DeviceConfiguration deviceConfig,
        IReadOnlyList<Reading> readings,
        CancellationToken cancellationToken = default);
}
