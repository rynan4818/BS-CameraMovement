using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BS_CameraMovement.Osc
{
    /*
      追加ファイル: Osc\OscServer.cs, Osc\OscPacketDecoder.cs, Osc\OscDataTypes.cs

      使い方

       using BS_CameraMovement.Osc;

       // 1. サーバー作成（受信ポート）
       var server = new OscServer(39550);

       // 2. コールバック登録
       server.OnMessageReceived += (OscMessage msg) =>
       {
           switch (msg.Address)
           {
               case "/camera/position":
                   float x = msg.GetFloat(0);
                   float y = msg.GetFloat(1);
                   float z = msg.GetFloat(2);
                   break;
               case "/camera/fov":
                   float fov = msg.GetFloat(0);
                   break;
               case "/camera/info":
                   string name = msg.GetString(0);   // "Camera"
                   float px = msg.GetFloat(1);       // pos.x ...
                   break;
           }
       };

       // 3. 受信開始
       server.Start();

       // 4. 終了時に停止
       server.Stop();   // or server.Dispose();

       注意: OnMessageReceivedはバックグラウンドスレッドから呼ばれます。Unity側で値を反映する場合はメインスレッドへのディスパッチが必要です（フィールドに保存してUpdate()で読む等）
    */
    
    /// <summary>
    /// OSC受信サーバー（UDPリスナー）
    /// </summary>
    public class OscServer : IDisposable
    {
        /// <summary>
        /// OSCメッセージ受信時のコールバック
        /// </summary>
        public event Action<OscMessage> OnMessageReceived;

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private bool _running;
        private readonly int _port;

        /// <summary>
        /// OscServerを作成する
        /// </summary>
        /// <param name="port">受信ポート番号</param>
        public OscServer(int port)
        {
            _port = port;
        }

        /// <summary>
        /// 受信を開始する
        /// </summary>
        public void Start()
        {
            if (_running) return;

            _udpClient = new UdpClient(_port);
            _running = true;

            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "OscServer"
            };
            _receiveThread.Start();
        }

        /// <summary>
        /// 受信を停止する
        /// </summary>
        public void Stop()
        {
            _running = false;

            if (_udpClient != null)
            {
                _udpClient.Close();
                _udpClient = null;
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(1000);
                _receiveThread = null;
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private void ReceiveLoop()
        {
            var endPoint = new IPEndPoint(IPAddress.Any, _port);

            while (_running)
            {
                try
                {
                    var data = _udpClient.Receive(ref endPoint);
                    if (data != null && data.Length > 0)
                    {
                        var message = OscPacketDecoder.Decode(data, data.Length);
                        if (message != null)
                        {
                            OnMessageReceived?.Invoke(message);
                        }
                    }
                }
                catch (SocketException)
                {
                    // UdpClient.Close()による正常終了
                    if (!_running) break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
    }
}
