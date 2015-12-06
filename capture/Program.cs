using System;

namespace capture
{
    class Program
    {
        static void Main(string[] args)
        {
            MitmMain mitm = null;
            try
            {
                Log.Info("---------- " + DateTime.Now + " --------------");

                mitm = new MitmMain();

                mitm.Show();

                mitm.Loop();
            }
            catch(Exception err)
            {
                Log.Info("Main() " + err.Message);
            }
            finally
            {
            }  
        }
    }
}
