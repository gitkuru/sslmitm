# sslmitm
SSL/TLS MITM Test Tool for .NET

LAN内のターゲット端末に対してMan In The Middle攻撃を行い
SSL/TLS通信の傍受/改ざんを行う簡易テストツールです。
動作には.NET Frameworkが必要です。


# 環境 Environment
- Windows 7/8
- .NET 4.6

# 機能
- LAN内のターゲットに対するARPポイズニング
- SSL/TLS通信のリクエスト/レスポンスの傍受
- スクリプトを用いて傍受パケットに任意の改ざん処理を追加

# 備考
- スキャン機能は実装されていません。
- 実行には事前にターゲットのIP/MACとゲートウェイのIP/MACを設定する必要があります。
  - "arp -a"コマンド等で簡易的に調べることができます。


