
namespace NetDaemon.Client.Common;

public interface IHomeAssistantApiManager
{
    /// <summary>
    ///     Get to Home Assistant API
    /// </summary>
    /// <param name="apiPath">relative path</param>
    /// <typeparam name="T">Return type (json serializable)</typeparam>
    Task<T?> GetApiCall<T>(string apiPath, CancellationToken cancelToken);

    /// <summary>
    ///     Post to Home Assistant API
    /// </summary>
    /// <param name="apiPath">relative path</param>
    /// <param name="data">data being sent</param>
    /// <typeparam name="T">Return type (json serializable)</typeparam>
    public Task<T?> PostApiCall<T>(string apiPath, CancellationToken cancelToken, object? data = null);
}
