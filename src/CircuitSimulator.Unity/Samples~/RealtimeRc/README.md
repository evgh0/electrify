# Realtime RC sample

Open `RealtimeRcSample.unity` and enter Play mode. The scene constructs a switched 5 V, 1 kΩ, 100 µF charging circuit and a normally-open discharge button through the high-level Unity API.

Opening/closing S1 and holding PB1 demonstrate state changes without a reset. The displayed simulation time and capacitor reading continue across those changes. Updating resistance still rebuilds at time zero, while deletion intentionally leaves an invalid circuit so the structured failure state is visible.
