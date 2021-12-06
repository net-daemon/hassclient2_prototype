# HassClient redesign prototype

This is the redesign of the HassClient. 

Designgoals:

- Better fit DI with better separation of conserns
- Simplify networking to be easier to manage
- Separate connected and non-connected logic
- Simplify interfaces and add extension methods 

Todo:

- [ ] Add another layer a HassClient runner that handles reconnect and expose the state change through IObservable
- [ ] Add tests
- [ ] Add integration tests
- [ ] Move to main netdaemon project

