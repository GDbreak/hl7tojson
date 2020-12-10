using NHapi.Base.Model;
using System;
using System.Collections.Generic;
using System.Text;
using HL7CL.ADT;

namespace HL7CL
{
    public class MessageFactory
    {
        public static IMessage CreateMessage(string messageType)
        {
            //This patterns enables you to build other message types
            if (messageType.Equals("A01"))
            {
                return new A01Builder().Build();
            }

            //if other types of ADT messages are needed, then implement your builders here
            throw new ArgumentException($"'{messageType}' is not supported yet. Extend this if you need to");
        }
    }
}
