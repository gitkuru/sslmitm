using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScriptRunnerLibrary;

namespace capture
{
    /// <summary>
    /// スクリプトの関数を使用して変換処理を実施する
    /// </summary>
    public class MitmScriptConverter : IConverter
    {
        private ScriptRunner Runner = null;
        
        private Object ObjectOnScript = null;

        private string ScriptCode = "";


        public MitmScriptConverter()
        {
            Log.Info("reading script_convert.cs...");
            using (var fs = new FileStream("script_convert.cs", FileMode.Open))
            {
                Log.Info("Success");
                var reader = new StreamReader(fs);
                ScriptCode = reader.ReadToEnd();
                compile();
            }
        }

        /// <summary>
        /// ターゲットからのリクエストを改ざんする
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public byte[] ConvertRequest(byte[] buff, int offset, int size)
        {
            try
            {
                // スクリプトの関数を呼び出す
                var converted_bytes = Runner.InvokeClassFunction("MitmConverter", "ConvertRequest", new object[] { buff, offset, size }) as byte[];
                return converted_bytes;
            }
            catch (Exception err)
            {
                Log.Error("ConvertRequest() " + err.Message);
                return buff;
            }
        }

        /// <summary>
        /// オリジナルサーバーからのレスポンスを改ざんする
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public byte[] ConvertResponse(byte[] buff, int offset, int size)
        {
            try
            {
                // スクリプトの関数を呼び出す
                var converted_bytes = Runner.InvokeClassFunction("MitmConverter", "ConvertResponse", new object[] { buff, offset, size }) as byte[];
                return converted_bytes;
            }
            catch(Exception err)
            {
                Log.Error("ConvertResponse() " + err.Message);
                return buff;
            }
        }

        private void compile()
        {
            // スクリプトをコンパイル
            Log.Info("Script Compile..");
            Runner = new ScriptRunnerLibrary.ScriptRunner(ScriptCode);

            if(Runner.Ready())
            {
                Log.Info("Success");
            }
            else
            {
                Log.Info("Compile Error");
                foreach (var errmsg in Runner.ErrorMessage())
                {
                    Log.Error("Error " + errmsg);
                }
                throw new Exception("compile error");
            }

            // インスタンスを取得
            ObjectOnScript = Runner.CreateInstance("MitmConverter", new object[] { } );
        }
    }
}
