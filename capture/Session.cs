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
    }
}
