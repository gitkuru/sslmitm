using System;
using System.Net.NetworkInformation;
using System.Net;

using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.WinPcap;
using PacketDotNet;

namespace capture
{
    /// <summary>
    /// PC上の全NICに関する情報を管理
    /// SharpPcapの処理を隠蔽
    /// </summary>
    public class NicInformation
    {
        /// <summary>
        /// システムにあるデバイスの数を返す
        /// </summary>
        /// <returns></returns>
        public static int GetInterfaceCount()
        {
            return WinPcapDeviceList.Instance.Count;
        }

        /// <summary>
        /// システムにあるデバイスの情報を返す
        /// </summary>
        /// <param name="i">インデックス</param>
        /// <returns></returns>
        public static string GetInterfaceString(int i)
        {
            var str = WinPcapDeviceList.Instance[i].ToString();
            return str;
        }

        /// <summary>
        /// システムにあるデバイスの名称を返す
        /// </summary>
        /// <param name="i">インデックス</param>
        /// <returns>名称文字列</returns>
        public static string GetInterfaceName(int i)
        {
            var name = WinPcapDeviceList.Instance[i].Name;
            return name;
        }

        /// <summary>
        /// システムにあるデバイスに割り当てられているIPアドレスを返す
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static IPAddress GetIpAddress(int i)
        {
            foreach (var addr in WinPcapDeviceList.Instance[i].Addresses)
            {
                if (checkIpV4(addr))
                {
                    return addr.Addr.ipAddress;
                }
            }

            return null;
        }

        /// <summary>
        /// システムにあるデバイスに割り当てられているMACアドレスを返す
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static PhysicalAddress GetPhysicalAddress(int i)
        {
            foreach (var addr in WinPcapDeviceList.Instance[i].Addresses)
            {
                if (checkHwAddress(addr))
                {
                    return addr.Addr.hardwareAddress;
                }
            }
            return null;
        }

        private static bool checkIpV4(PcapAddress addr)
        {
            // ARPを送って送信先MACアドレスを取得したいので、IPv4アドレスを探す
            // ネットマスクの有無をみる簡易判定
            return (addr.Netmask.ToString() != "");
        }

        private static bool checkHwAddress(PcapAddress addr)
        {
            // ARPを送って送信先MACアドレスを取得したいので、IPv4アドレスを探す
            // ネットマスクの有無をみる簡易判定
            return (addr.Netmask == null);
        }
    }


    /// <summary>
    /// NICクラス
    /// </summary>
    public class NicInterface : IDisposable
    {
        /// <summary>
        /// Pcapデバイス
        /// </summary>
        public WinPcapDevice Device { get; private set; }

        /// <summary>
        /// OSに割り振られたインターフェースのインデックス
        /// </summary>
        public int InterfaceIndex { get; private set; }

        /// <summary>
        /// 自分自身のIPアドレス
        /// </summary>
        public IPAddress LocalAddress { get; private set; }

        /// <summary>
        /// 自分自身のMACアドレス
        /// </summary>
        public PhysicalAddress MacAddress { get; private set; }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="index">キャプチャ対象のインターフェースインデックス</param>
        public NicInterface(int index)
        {
            Device = WinPcapDeviceList.Instance[index];

            LocalAddress = NicInformation.GetIpAddress(index);
            MacAddress = NicInformation.GetPhysicalAddress(index);

            InterfaceIndex = index;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// デバイスをオープンする
        /// </summary>
        public void Open()
        {
            Device.Open(DeviceMode.Promiscuous, 1);
            Device.Filter = "(arp || ip || ip6)";
            Device.OnPacketArrival += new PacketArrivalEventHandler(onPacketArrival);
        }

        /// <summary>
        /// キャプチャを開始する
        /// </summary>
        public void StartCapture()
        {
            if (Device.Opened)
            {
                Device.StartCapture();
            }
        }

        /// <summary>
        /// キャプチャを停止する
        /// </summary>
        public void StopCapture()
        {
            if (Device.Opened)
            {
                Device.StopCapture();
                Device.Close();
            }
        }

        /// <summary>
        /// パケットを送信する
        /// 各パケットのペイロードに引数のパケットを乗せる
        /// </summary>
        /// <param name="ethernetPacket"></param>
        /// <param name="ipPacket"></param>
        /// <param name="tcpPacket"></param>
        public void SendPacket(EthernetPacket ethernetPacket, IpPacket ipPacket, TcpPacket tcpPacket)
        {
            var ipv4Packet = (IPv4Packet)ipPacket;

            if (tcpPacket != null)
            {
                ipPacket.PayloadPacket = tcpPacket;
                tcpPacket.UpdateTCPChecksum();

                if (tcpPacket.Checksum != tcpPacket.CalculateTCPChecksum())
                {
                }
            }

            if (ipPacket != null)
            {
                ethernetPacket.PayloadPacket = ipPacket;
                ipv4Packet.UpdateIPChecksum();

                if (ipv4Packet.Checksum != ipv4Packet.CalculateIPChecksum())
                {
                }
            }

            // パケット送信
            Device.SendPacket(ethernetPacket);
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~NicInterface()
        {
            Dispose(false);
        }

        private void Dispose(bool is_managed_resource)
        {
            if (is_managed_resource)
            {
            }
            else
            {
                Device.Close();
            }
        }

        /// <summary>
        /// キャプチャコールバック
        /// パケットをキャプチャする毎にコールされる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPacketArrival(Object sender, CaptureEventArgs e)
        {
            try
            {
                Packet packet;

                try
                {
                    packet = Packet.ParsePacket(LinkLayers.Ethernet, e.Packet.Data);
                }
                catch
                {
                    return;
                }

                
                if (packet is PacketDotNet.EthernetPacket)
                {
                    TcpPacket tcp = TcpPacket.GetEncapsulated(packet);
                    IpPacket ip = IpPacket.GetEncapsulated(packet);
                    EthernetPacket eth = EthernetPacket.GetEncapsulated(packet);

                    PacketTamper.OnPacketArrival(this, eth, ip, tcp);
                }
            }
            catch(Exception err)
            {
                Log.Error("onPacketArrival() " + err.Message);
            }
        }
    }
}
