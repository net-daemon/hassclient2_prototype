[![Coverage Status](https://coveralls.io/repos/github/net-daemon/hassclient2_prototype/badge.svg)](https://coveralls.io/github/net-daemon/hassclient2_prototype)

# HassClient redesign prototype


This is the redesign of the HassClient. 

Designgoals:

- Better fit DI with better separation of conserns
- Simplify networking to be easier to manage
- Separate connected and non-connected logic
- Simplify interfaces and add extension methods
- Remove any dynamic

Todo:

- [x] Add another layer a HassClient runner that handles reconnect and expose the state change through IObservable
- [x] Add tests
- [x] Add integration tests
- [ ] Move to main netdaemon project

## The public API design

| Interface  | Description  |
|---|---|
| IHomeAssistantClient  | This is the interface where you instance when you want to connect to a Home Assistant instance  |
| IHomeAssistantRunner | To be coded, Maintains connection to HomeAssistant when connection breaks |
| IHomeAssistantConnection  | Returned by ConnectAsync of HomeAssistantClient. It has basic methods to send commands and receive results. It also will have basic methods to use the http/json API of Home Assistant  |
| IHomeAssistantConnectionExtensions | Extension methods on top of IHomeAssistantConnection that will allow for getting services, devices, areas etc. |
| ServiceCollectionExtensions | Extension methods add services to service collection  |