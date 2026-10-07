using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Receives webcam pose-detection results from the companion Python script
/// via UDP on localhost:8765.
///
/// Gesture mapping (from Python MediaPipe Hands):
///   • Right hand index finger raised  → run right  (HorizontalAxis = +1)
///   • Left  hand index finger raised  → run left   (HorizontalAxis = -1)
///   • Two hands open raised           → reaching / catching stars
///
/// The Python side sends a tiny JSON payload at ~30 Hz:
///   { "gesture": "right" | "left" | "reach" | "idle" }
///
/// This component is optional — if no pose server is running,
/// the game falls back to keyboard input (handled by PlayerInput).
/// </summary>
public class WebcamPoseInput : MonoBehaviour
{
    [Header("UDP")]
    [SerializeField] private int listenPort = 8765;
    [SerializeField, Min(0.5f)] private float timeoutSeconds = 2f;

    /// <summary>True when receiving pose data.</summary>
    public bool IsActive { get; private set; }

    /// <summary>–1 = left, 0 = idle, +1 = right.</summary>
    public float HorizontalAxis { get; private set; }

    /// <summary>True when the player wants to catch/collect (both hands open).</summary>
    public bool IsReaching { get; private set; }

    // ------------------------------------------------------------------ //
    //  Internal state
    // ------------------------------------------------------------------ //
    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running;
    private volatile string latestGesture = "idle";

    // Track when the last packet was received (set on background thread,
    // read on main thread). We use frame count instead of Time.time because
    // Time.time cannot be accessed from a background thread.
    private volatile bool receivedThisSecond;
    private float timeSinceLastReceive;

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    private void OnEnable()
    {
        try
        {
            udpClient = new UdpClient(listenPort);
            udpClient.Client.ReceiveTimeout = 100;
            running = true;
            receivedThisSecond = false;
            timeSinceLastReceive = timeoutSeconds + 1f; // Start as inactive

            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "PoseUDPReceiver"
            };
            receiveThread.Start();
            Debug.Log($"[WebcamPoseInput] Listening on UDP port {listenPort}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebcamPoseInput] Could not start: {ex.Message}. Keyboard fallback active.");
        }
    }

    private void OnDisable()
    {
        running = false;
        udpClient?.Close();
        receiveThread?.Join(500);
        IsActive = false;
    }

    private void Update()
    {
        // Check if the background thread has received any data recently.
        if (receivedThisSecond)
        {
            receivedThisSecond = false;
            timeSinceLastReceive = 0f;
        }
        else
        {
            timeSinceLastReceive += Time.deltaTime;
        }

        IsActive = timeSinceLastReceive < timeoutSeconds;

        // Apply the latest gesture on the main thread.
        string gesture = latestGesture;
        switch (gesture)
        {
            case "right":
                HorizontalAxis = 1f;
                IsReaching = false;
                break;
            case "left":
                HorizontalAxis = -1f;
                IsReaching = false;
                break;
            case "reach":
                HorizontalAxis = 0f;
                IsReaching = true;
                break;
            default: // "idle"
                HorizontalAxis = 0f;
                IsReaching = false;
                break;
        }
    }

    // ------------------------------------------------------------------ //
    //  UDP receive thread
    // ------------------------------------------------------------------ //

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string json = Encoding.UTF8.GetString(data);
                latestGesture = ParseGesture(json);
                receivedThisSecond = true; // Flag for main thread
            }
            catch (SocketException)
            {
                // Timeout — normal, just loop.
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Minimal JSON parse for {"gesture":"value"}.
    /// </summary>
    private static string ParseGesture(string json)
    {
        const string key = "\"gesture\"";
        int keyIdx = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIdx < 0) return "idle";

        int colonIdx = json.IndexOf(':', keyIdx + key.Length);
        if (colonIdx < 0) return "idle";

        int firstQuote = json.IndexOf('"', colonIdx + 1);
        if (firstQuote < 0) return "idle";

        int lastQuote = json.IndexOf('"', firstQuote + 1);
        if (lastQuote < 0) return "idle";

        return json.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
    }
}
