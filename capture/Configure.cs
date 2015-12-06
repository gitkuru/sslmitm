using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;


namespace capture
{
    public class Configure
    {
        /// <summary>
        /// キャプチャするインターフェースインデックス
        /// </summary>
        public static int InterfaceIndex = 0;

        /// <summary>
        /// ターゲットIP
        /// </summary>
        public static IPAddress TargetIP;
        public static IPAddress GatewayIP;
        public static PhysicalAddress TargetMac;
        public static PhysicalAddress GatewayMac;

        /// <summary>
        /// 受信バッファサイズ
        /// </summary>
        public static int ReadBufferSize = -1;


        /// <summary>
        /// ターゲットへ転送するまでバッファリングする最小サイズ
        /// </summary>
        public static int MinimumSizeToTarget = -1;

        /// <summary>
        /// オリジナルサーバーへ転送するまでバッファリングする最小サイズ
        /// </summary>
        public static int MinimumSizeToOriginalServer = -1;

        /// <summary>
        /// 本来接続するサーバーのホスト名
        /// </summary>
        /// <remarks>
        /// 現在未使用(将来の利用を想定)
        /// </remarks>
        public static string ServerHostName = "";

        /// <summary>
        /// 証明書ファイル(PFX形式)の名称
        /// </summary>
        public static string CertificateFileName = "server.pfx";

        /// <summary>
        /// 証明書ファイルのパスワード
        /// </summary>
        public static string CertificateFilePassword = "test";

        /// <summary>
        /// 検査するポート
        /// </summary>
        public static ushort InspectTargetSslPort = 443;


        /// <summary>
        /// コンフィグファイルを読み込む
        /// </summary>
        public static void Load()
        {
            configureFromSettingFile();
            configureCheck();
        }


        /// <summary>
        /// 設定ファイルから設定値を読み込む
        /// </summary>
        private static void configureFromSettingFile()
        {
            using (var fs = new FileStream("setting.txt", FileMode.Open))
            {
                using (var reader = new StreamReader(fs))
                {
                    while (!reader.EndOfStream)
                    {
                        // 一行ずつ読み込み「=」で分割する
                        var line = reader.ReadLine();

                        // #はコメント行とする
                        if (line.Length == 0 || line[0] == '#')
                        {
                            continue;
                        }

                        var param = line.Split('=');

                        if (param.Length >= 2)
                        {
                            var type = param[0];
                            var value = param[1];

                            Log.Info("Load " + type);
                            switch (type)
                            {
                                case "CertificateFileName":
                                    Configure.CertificateFileName = value;
                                    break;

                                case "CertificateFilePassword":
                                    Configure.CertificateFilePassword = value;
                                    break;

                                case "InspectTargetSslPort":
                                    Configure.InspectTargetSslPort = ushort.Parse(value);
                                    break;

                                case "InterfaceIndex":
                                    Configure.InterfaceIndex = int.Parse(value);
                                    break;

                                case "TargetIP":
                                    Configure.TargetIP = IPAddress.Parse(value);
                                    break;

                                case "GatewayIP":
                                    Configure.GatewayIP = IPAddress.Parse(value);
                                    break;

                                case "TargetMAC":
                                    Configure.TargetMac = PhysicalAddress.Parse(value);
                                    break;

                                case "GatewayMAC":
                                    Configure.GatewayMac = PhysicalAddress.Parse(value);
                                    break;

                                case "ServerHostName":
                                    Configure.ServerHostName = value;
                                    break;

                                case "MinimumSizeToTarget":
                                    Configure.MinimumSizeToTarget = int.Parse(value);
                                    break;

                                case "MinimumSizeToOriginalServer":
                                    Configure.MinimumSizeToOriginalServer = int.Parse(value);
                                    break;

                                case "ReadBufferSize":
                                    Configure.ReadBufferSize = int.Parse(value);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 設定ファイルから必要情報が読み込めたか確認する
        /// </summary>
        private static void configureCheck()
        {
            if (Configure.InterfaceIndex >= NicInformation.GetInterfaceCount())
            {
                throw new Exception("Invalid Interface Index");
            }

            if (Configure.TargetIP == null)
            {
                throw new Exception("Invalid Target IP");
            }

            if (Configure.GatewayIP == null)
            {
                throw new Exception("Invalid Gateway IP");
            }

            if (Configure.TargetMac == null)
            {
                throw new Exception("Invalid Target MAC");
            }

            if (Configure.GatewayMac == null)
            {
                throw new Exception("Invalid Gateway MAC");
            }

            if (Configure.ServerHostName == "")
            {
                throw new Exception("Invalid Server Host Name");
            }

            if(Configure.MinimumSizeToTarget < 0 )
            {
                throw new Exception("Invalid MinimumSizeToTarget");
            }

            if(Configure.MinimumSizeToOriginalServer < 0)
            {
                throw new Exception("Invalid MinimumSizeToOriginalServer");
            }

            if (Configure.ReadBufferSize < 0)
            {
                throw new Exception("Invalid ReadBufferSize");
            }
        }
    }
}
