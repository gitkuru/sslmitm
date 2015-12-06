public class MitmConverter: System.MarshalByRefObject
{
    public MitmConverter()
    {
    }

    /// <summary>
    /// Convert Request Message
    /// </summary>
    /// <param name="request"></param>
    /// <param name="offset"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static byte[] ConvertRequest(byte[] request, int offset, int size)
    {
        byte[] converted_bytes = request;

        if (enableConvertRequest)
        {
            try
            {
                // Assume response is HTTP String Message 
                string request_str = System.Text.Encoding.UTF8.GetString(request, 0, size);

                // Cutomize function
                string converted_str = convertRequest(request_str);

                converted_bytes = System.Text.Encoding.UTF8.GetBytes(converted_str);
            }
            catch (System.Exception)
            {
                converted_bytes = request;
            }
        }

        return converted_bytes;
    }

    /// <summary>
    /// Convert Response Message
    /// </summary>
    /// <param name="response">response data from original server</param>
    /// <returns>converted response to target</returns>
    public static byte[] ConvertResponse(byte[] response, int offset, int size)
    {
        byte[] converted_bytes = response;

        if (enableConvertResponse)
        {
            try
            {
                // Assume response is HTTP String Message 
                string response_str = System.Text.Encoding.UTF8.GetString(response, 0, size);

                // Cutomize function
                string converted_str = convertResponse(response_str);

                converted_bytes = System.Text.Encoding.UTF8.GetBytes(converted_str);
            }
            catch (System.Exception)
            {
                converted_bytes = response;
            }
        }

        return converted_bytes;
    }



    //-------------------------------------------------------------------------
    // Cutomize Function
    //-------------------------------------------------------------------------

    /// <summary>
    /// Enable/Disable Request
    /// </summary>
    private static bool enableConvertRequest = false;

    /// <summary>
    /// Enable/Disable Resonse
    /// </summary>
    private static bool enableConvertResponse = false;

    /// <summary>
    /// convert request
    /// </summary>
    /// <param name="requestStr">Request Message from Target</param>
    /// <returns>Request Message for Original Server</returns>
    private static string convertRequest(string requestStr)
    {
        return requestStr;
    }

    /// <summary>
    /// convert response
    /// </summary>
    /// <param name="responseStr">Response Message from Original Server</param>
    /// <returns>Response Message for Target</returns>
    private static string convertResponse(string responseStr)
	{
		string rep = responseStr.Replace("200 OK", "202 OK");
        return rep;
	}
}
