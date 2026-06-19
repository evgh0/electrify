# Realtime RC sample

Open `RealtimeRcSample.unity` and enter Play mode. The scene constructs a 5 V, 1 kΩ, 100 µF charging circuit through the high-level Unity API.

The on-screen controls update resistance or delete the resistor and its attached wires. Each edit rebuilds the immutable Core circuit at time zero. Deletion intentionally leaves an invalid circuit so the manager's structured failure state is visible.
