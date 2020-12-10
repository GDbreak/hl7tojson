using NHapi.Base.Model;
using NHapi.Base.Parser;
using System;
using System.IO;
using System.Text;

namespace HL7CL
{
    public class MessageController
    {
        private static int PORT_NUMBER = 63605;
        public static void Main(String[] args)
        {
            try
            {
                // create the HL7 message
                // this AdtMessageFactory class is not from NHAPI but my own wrapper
                // check my GitHub page or see my earlier article for reference
                var adtMessage = MessageFactory.CreateMessage("A01");

                // create a new MLLP client over the specified port (note this class is from NHAPI Tools)
                //Note that using higher level encodings such as UTF-16 is not recommended due to conflict with
                //MLLP wrapping characters

                //var connection = new SimpleMLLPClient("localhost", PORT_NUMBER, Encoding.UTF8);

                // send the previously created HL7 message over the connection established
                var parser = new PipeParser();
                LogToDebugConsole("Sending message:" + "\n" + parser.Encode(adtMessage));
                ////var response = connection.SendHL7Message(adtMessage);

                // display the message response received from the remote party
                ////var responseString = parser.Encode(response);
                //LogToDebugConsole("Received response:\n" + responseString);

            }
            catch (Exception e)
            {
                LogToDebugConsole($"Error occured while creating HL7 message {e.Message}");
            }

        }


        private static void WriteMessageFile(ParserBase parser, IMessage hl7Message, string outputDirectory, string outputFileName)
        {
            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var fileName = Path.Combine(outputDirectory, outputFileName);

            LogToDebugConsole("Writing data to file...");

            if (File.Exists(fileName))
                File.Delete(fileName);
            File.WriteAllText(fileName, parser.Encode(hl7Message));
            LogToDebugConsole($"Wrote data to file {fileName} successfully...");
        }

        private static void LogToDebugConsole(string informationToLog)
        {
            Console.WriteLine(informationToLog);
        }
    }
}
