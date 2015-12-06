using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;

using SharpPcap.WinPcap;
using PacketDotNet;

namespace capture
{
    class ArpUtility
    {
        /// <summary>
        /// ARP送信対象のインターフェース
        /// </summary>
        private NicInterface Interface;

        /// <summary>
        /// 送信するARPレスポンス群
        /// 先頭パケットから順番に送信する
        /// </summary>
        private List<EthernetPacket> ArpResponsePackets = new List<EthernetPacket>();

        /// <summary>
        /// ARPレスポンスを送信するインターバル
        /// </summary>
        public int ArpResponseTimeInterval = 2500;

        /// <summary>
        /// ARPレスポンスの送信を有効にする
        /// </summary>
        public bool EnableArpResponseSend = true;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="nic"></param>
        public ArpUtility(NicInterface nic)
        {
            Interface = nic;
        }


        /// <summary>
        /// ARPレスポンスを追加する
        /// </summary>
        /// <param name="targetMAC"></param>
        /// <param name="targetIP"></param>
        /// <param name="srcMAC"></param>
        /// <param name="responseIP"></param>
        public void AddArpResponse(PhysicalAddress targetMAC, IPAddress targetIP, PhysicalAddress srcMAC, IPAddress responseIP)
        {
            var ethernetPacket = new EthernetPacket(srcMAC, targetMAC, EthernetPacketType.Arp);
            var arpPacket = new ARPPacket(ARPOperation.Response, targetMAC, targetIP, srcMAC, responseIP);
            ethernetPacket.PayloadPacket = arpPacket;

            ArpResponsePackets.Add(ethernetPacket);
        }


        /// <summary>
        /// ARPレスポンス送信ループ
        /// </summary>
        /// <param name="nic"></param>
        /// <param name="queue"></param>
        public void StartArpResponseLoop()
        {
            try
            {
                var device = Interface.Device;
                var interval = ArpResponseTimeInterval;
                var size = (120) * ArpResponsePackets.Count;
                var sendQueue = new SendQueue(size);

                foreach (var eth in ArpResponsePackets)
                {
                    sendQueue.Add(eth.Bytes);
                }

                // Loop
                while (EnableArpResponseSend)
                {
                    sendQueue.Transmit(device, SendQueueTransmitModes.Normal);

                    Thread.Sleep(interval);
                }
            }
            catch (Exception err)
            {
                Log.Error("StartArpResponseLoop() " + err.Message);
            }
        }
    }
}
