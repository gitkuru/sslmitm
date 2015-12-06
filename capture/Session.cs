using System.Collections.Generic;
using System.Net;

namespace capture
{
    /// <summary>
    /// セッション
    /// </summary>
    class Session
    {
        public IPAddress SourceAddress { get; set; }
        public IPAddress DestinationAddress { get; set; }
        public ushort SourcePort { get; set; }
        public ushort DestinationPort { get; set; }

        public Session(IPAddress srcIp, ushort srcPort, IPAddress dstIp, ushort dstPort)
        {
            SourceAddress = srcIp;
            SourcePort = srcPort;
            DestinationAddress = dstIp;
            DestinationPort = dstPort;
        } 
    }

    /// <summary>
    /// ターゲットから接続されたセッション情報を管理
    /// </summary>
    class HookManager
    {
        /// <summary>
        /// ターゲットから接続されたセッション情報
        /// ソースポート毎に保存する
        /// </summary>
        public static List<Session> HookSessions = new List<Session>();


        public static void SetHookSession(Session s, ushort port)
        {
            int count = HookManager.HookSessions.Count;
            for (int i = 0; i < count; i++)
            {
                if (HookManager.HookSessions[i].SourcePort == port)
                {
                    HookManager.HookSessions.Remove(HookManager.HookSessions[i]);
                    break;
                }
            }
            HookManager.HookSessions.Add(s);
        }
    }
}
