using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeyondSlides
{
    /// <summary>Mensaje JSON que viaja entre Unity, el servidor y la web.</summary>
    [Serializable]
    public class NetMessage
    {
        public string type;      // hello | state | ping | info | end | reset
        public string role;      // presenter | audience
        public int station;
        public string title;
        public string[] targets; // ids de los PingTarget de la estación actual
        public string[] labels;  // textos de los botones
        public string target;    // id al que se hizo ping
        public int count;
        public int clients;      // asistentes conectados
        public string url;       // dirección que debe abrir la audiencia
    }

    /// <summary>
    /// Puente en tiempo real con la audiencia: se conecta por WebSocket al servidor
    /// (carpeta Server), publica la estación actual y recibe los pings.
    /// Si el servidor no está encendido, reintenta solo cada pocos segundos.
    /// </summary>
    public class AudienceClient : MonoBehaviour
    {
        public static AudienceClient Instance { get; private set; }

        [Tooltip("En el mismo PC: ws://localhost:8080. En Quest: ws://IP-DEL-PC:8080")]
        public string serverUrl = "ws://localhost:8080";
        [Tooltip("Texto del hub donde se muestra la dirección para la audiencia.")]
        public TextMesh infoText;

        public bool Connected { get { return connected; } }
        public int AudienceCount { get; private set; }
        public int TotalPings { get; private set; }

        readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
        readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        CancellationTokenSource cts;
        volatile ClientWebSocket socket;
        volatile bool connected;
        bool wasConnected;
        string audienceUrl = "";

        void Awake() { Instance = this; }

        void OnEnable()
        {
            cts = new CancellationTokenSource();
            var token = cts.Token;
            string url = serverUrl;
            Task.Run(() => RunLoop(url, token));
            UpdateInfoText();
        }

        void OnDisable()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
            connected = false;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (connected != wasConnected)
            {
                wasConnected = connected;
                if (connected)
                {
                    Debug.Log("[BeyondSlides] Conectado al servidor de audiencia: " + serverUrl);
                    Send(new NetMessage { type = "hello", role = "presenter" });
                    SendState();
                }
                else
                {
                    Debug.Log("[BeyondSlides] Sin conexión con el servidor de audiencia (se reintenta).");
                }
                UpdateInfoText();
            }

            string raw;
            while (inbox.TryDequeue(out raw)) Handle(raw);
        }

        // ---------- Envío ----------

        /// <summary>Publica la estación actual y sus botones para la audiencia.</summary>
        public void SendState()
        {
            var mgr = StationManager.Instance;
            var st = mgr != null ? mgr.Current : null;
            var msg = new NetMessage { type = "state", role = "presenter" };
            if (st != null)
            {
                msg.station = st.index;
                msg.title = st.title;
                int n = st.pingTargets.Count;
                msg.targets = new string[n];
                msg.labels = new string[n];
                for (int i = 0; i < n; i++)
                {
                    msg.targets[i] = st.pingTargets[i] != null ? st.pingTargets[i].id : "";
                    msg.labels[i] = st.pingTargets[i] != null ? st.pingTargets[i].label : "";
                }
            }
            else
            {
                msg.station = -1;
                msg.title = "";
                msg.targets = new string[0];
                msg.labels = new string[0];
            }
            Send(msg);
        }

        public void SendEnd()
        {
            Send(new NetMessage { type = "end", role = "presenter" });
        }

        public void Send(NetMessage msg)
        {
            var ws = socket;
            if (!connected || ws == null) return;
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(msg));
            var token = cts != null ? cts.Token : CancellationToken.None;
            Task.Run(() => SendAsync(ws, bytes, token));
        }

        async Task SendAsync(ClientWebSocket ws, byte[] bytes, CancellationToken token)
        {
            try
            {
                await sendLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (ws.State == WebSocketState.Open)
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token)
                            .ConfigureAwait(false);
                }
                finally { sendLock.Release(); }
            }
            catch (Exception) { /* se pierde el mensaje; el estado se reenvía al reconectar */ }
        }

        // ---------- Recepción ----------

        async Task RunLoop(string url, CancellationToken token)
        {
            var buffer = new byte[8192];
            while (!token.IsCancellationRequested)
            {
                var ws = new ClientWebSocket();
                try
                {
                    await ws.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
                    socket = ws;
                    connected = true;

                    while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                    {
                        using (var ms = new MemoryStream())
                        {
                            WebSocketReceiveResult result;
                            bool closed = false;
                            do
                            {
                                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token)
                                    .ConfigureAwait(false);
                                if (result.MessageType == WebSocketMessageType.Close) { closed = true; break; }
                                ms.Write(buffer, 0, result.Count);
                            } while (!result.EndOfMessage);

                            if (closed) break;
                            inbox.Enqueue(Encoding.UTF8.GetString(ms.ToArray()));
                        }
                    }
                }
                catch (Exception) { /* servidor apagado o conexión caída: se reintenta abajo */ }
                finally
                {
                    connected = false;
                    socket = null;
                    try { ws.Dispose(); } catch (Exception) { }
                }

                try { await Task.Delay(3000, token).ConfigureAwait(false); }
                catch (Exception) { }
            }
        }

        void Handle(string raw)
        {
            NetMessage msg;
            try { msg = JsonUtility.FromJson<NetMessage>(raw); }
            catch (Exception) { return; }
            if (msg == null || string.IsNullOrEmpty(msg.type)) return;

            switch (msg.type)
            {
                case "ping":
                    if (PingTarget.Dispatch(msg.target)) TotalPings++;
                    break;

                case "info":
                    AudienceCount = msg.clients;
                    if (!string.IsNullOrEmpty(msg.url)) audienceUrl = msg.url;
                    UpdateInfoText();
                    break;
            }
        }

        void UpdateInfoText()
        {
            if (infoText == null) return;
            if (!connected)
                infoText.text = "Audiencia: servidor desconectado\n(inicia la carpeta Server)";
            else
                infoText.text = "Participa desde tu celular:\n" + audienceUrl +
                                "\nAsistentes conectados: " + AudienceCount;
        }
    }
}
