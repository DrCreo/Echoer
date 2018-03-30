using System;
using System.IO;
using System.Reflection;


public class LogWriter
{
    private string m_exePath = string.Empty;


    public LogWriter(string logMessage)
    {
        LogWrite(logMessage);
    }


    public void LogWrite(string logMessage)
    {
        m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        try
        {
            using (StreamWriter w = File.AppendText(/*m_exePath + "\\" + */"log.txt"))
            {
                Log(logMessage, w);
            }
        }
        catch (Exception ex)
        {
        }
    }

    public void Log(string logMessage, TextWriter txtWriter)
    {
        try
        {
            var st = logMessage.Split("\n");

            txtWriter.Write("\rLog Entry : ");
            txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            txtWriter.WriteLine("  -");
            foreach (var m in st)
            {
                if (m != "")
                    txtWriter.WriteLine("  -{0}", m);
            }
            txtWriter.WriteLine("-------------------------------");
        }
        catch (Exception ex)
        {
        }
    }
}