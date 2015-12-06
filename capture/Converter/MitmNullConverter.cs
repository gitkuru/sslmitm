namespace capture
{
    /// <summary>
    /// デフォルトの改ざん処理(何もしない)
    /// </summary>
    class MitmNullConverter : IConverter
    {
        public byte[] ConvertRequest(byte[] buff, int offset, int size)
        {
            return buff;
        }

        public byte[] ConvertResponse(byte[] buff, int offset, int size)
        {
            return buff;
        }
    }
}
