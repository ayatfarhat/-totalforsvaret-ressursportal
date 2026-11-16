using Microsoft.AspNetCore.SignalR;

namespace Nabohjelp.Hubs;

// Håndterer tilkoblinger fra nettlesere.
// Serveren bruker denne til å varsle om endringer i ressurser og behov.
public class OppdateringsHub : Hub
{
}