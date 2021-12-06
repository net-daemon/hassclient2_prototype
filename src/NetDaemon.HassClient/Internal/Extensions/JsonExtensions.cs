﻿using NetDaemon.Client.Common.HomeAssistant.Model;
using NetDaemon.Client.Internal.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace NetDaemon.Client.Internal.Extensions;
internal static class JsonExtensions
{
    private static readonly JsonSerializerOptions SnakeCaseNamingPolicySerializerOptions = new()
    {
        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
    };

    [return: MaybeNull]
    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions? options = null)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            element.WriteTo(writer);
        }

        return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options) ?? default!;
    }
    public static JsonElement? ToJsonElement<T>(this T source, JsonSerializerOptions? options = null)
    {
        if (source == null) return null;
        var json = JsonSerializer.Serialize<T>(source, options);
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    ///     Parses all json elements to instance result from GetServices call
    /// </summary>
    /// <param name="element">JsonElement containing the result data</param>
    public static IReadOnlyCollection<HassServiceDomain> ToServicesResult(this JsonElement element)
    {
        var result = new List<HassServiceDomain>();
        Type[] executingAssemblyTypes = Assembly.GetExecutingAssembly().GetTypes();

        if (element.ValueKind != JsonValueKind.Object)
            throw new ApplicationException("Not expected result from the GetServices result");

        foreach (var property in element.EnumerateObject())
        {
            var serviceDomain = new HassServiceDomain
            {
                Domain = property.Name,
                Services = getServices(property.Value)
            };
            result.Add(serviceDomain);
        }

        IReadOnlyCollection<HassService> getServices(JsonElement element)
        {
            var servicesList = new List<HassService>();
            foreach (var serviceDomainProperty in element.EnumerateObject())
            {
                servicesList.Add(getServiceFields(serviceDomainProperty.Name, serviceDomainProperty.Value));
            }
            return servicesList;
        }

        HassService getServiceFields(string service, JsonElement element)
        {
            var serviceFields = new List<HassServiceField>();

            string? serviceDescription = null;
            TargetSelector? target = null;

            foreach (var serviceProperty in element.EnumerateObject())
            {
                switch (serviceProperty.Name)
                {
                    case "description":
                        serviceDescription = serviceProperty.Value.GetString();
                        break;
                    case "fields":
                        foreach (var fieldsProperty in serviceProperty.Value.EnumerateObject())
                        {
                            serviceFields.Add(getField(fieldsProperty.Name, fieldsProperty.Value));
                        }
                        break;
                    case "target":
                        target = getSelector(serviceProperty.Name, serviceProperty.Value) as TargetSelector;
                        break;
                }
            }
            return new HassService
            {
                Service = service,
                Fields = serviceFields,
                Description = serviceDescription,
                Target = target
            };
        }

        object? getSelector(string selectorName, JsonElement element)
        {
            var selectorType = executingAssemblyTypes.FirstOrDefault(x => string.Equals($"{selectorName}Selector", x.Name, StringComparison.OrdinalIgnoreCase));

            if (selectorType is null)
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.Object && !element.EnumerateObject().Any() ||
                element.ValueKind != JsonValueKind.Object && element.GetString() is null)
            {
                return Activator.CreateInstance(selectorType);
            }

            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                element.WriteTo(writer);
            }

            return JsonSerializer.Deserialize(bufferWriter.WrittenSpan,
                    selectorType,
                    SnakeCaseNamingPolicySerializerOptions);
        }

        HassServiceField getField(string fieldName, JsonElement element)
        {
            object? example = null;
            string? fieldDescription = null;
            bool? required = null;
            object? selector = null;

            foreach (var fieldProperty in element.EnumerateObject())
            {
                switch (fieldProperty.Name)
                {
                    case "description":
                        fieldDescription = fieldProperty.Value.GetString();
                        break;
                    case "required":
                        required = fieldProperty.Value.GetBoolean();
                        break;
                    case "example":
                        switch (fieldProperty.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                example = fieldProperty.Value.GetString();
                                break;
                            case JsonValueKind.Number:
                                if (fieldProperty.Value.TryGetInt64(out long longVal))
                                    example = longVal;
                                else
                                    example = fieldProperty.Value.GetDouble();
                                break;
                            case JsonValueKind.Object:

                                example = fieldProperty.Value;
                                break;
                            case JsonValueKind.True:
                                example = true;
                                break;
                            case JsonValueKind.False:
                                example = false;
                                break;
                            case JsonValueKind.Array:
                                example = fieldProperty.Value;
                                break;
                        }
                        break;
                    case "selector":
                        var selectorProperty = fieldProperty.Value.EnumerateObject().First();
                        selector = getSelector(selectorProperty.Name, selectorProperty.Value);
                        break;
                }
            }
            return new HassServiceField
            {
                Field = fieldName,
                Example = example,
                Description = fieldDescription,
                Required = required,
                Selector = selector
            };
        }

        return result;
    }
}