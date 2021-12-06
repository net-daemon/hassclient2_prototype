using NetDaemon.Client.Common.HomeAssistant.Model;
using NetDaemon.Client.Internal.Extensions;

namespace NetDaemon.Client.Common.HomeAssistant.Extensions;
public static class HassEventExtensions
{
    public static HassStateChangedEventData? ToStateChangedEvent(this HassEvent hassEvent)
    {
        var JsonElement = hassEvent?.DataElement ??
            throw new NullReferenceException("DataElement cannot be empty");
        return JsonElement.ToObject<HassStateChangedEventData>();
    }
}