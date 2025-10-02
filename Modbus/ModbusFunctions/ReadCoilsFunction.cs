using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            ModbusReadCommandParameters paranCon = this.CommandParameters as ModbusReadCommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy(
                    (Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.TransactionId)), 0,(Array)request,0,2);                 

            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.ProtocolId)), 0, (Array)request, 2, 2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.Length)), 0, (Array)request, 4, 2);
            request[6] = paranCon.UnitId;
            request[7] = paranCon.FunctionCode;
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.StartAddress)), 0, (Array)request, 8, 2);
            Buffer.BlockCopy((Array)BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)paranCon.Quantity)), 0, (Array)request, 10, 2);

            return request; 
                            //throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
            
            ModbusReadCommandParameters paramCon = this.CommandParameters as ModbusReadCommandParameters;

            Dictionary<Tuple<PointType, ushort>, ushort> d = new Dictionary<Tuple<PointType, ushort>, ushort>();

            int q = response[8];

            
            for (int i = 0; i < q; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    
                    if ((j + i * 8) >= paramCon.Quantity)
                    {
                        break;
                    }

                    ushort v = (ushort)(response[9 + i] & 0x01);    

                    response[9 + i] /= 1;   
                    d.Add(                                  
                        new Tuple<PointType, ushort>(           
                            PointType.DIGITAL_OUTPUT,
                            (ushort)(paramCon.StartAddress + (j + i * 8))
                        ),
                        v
                    );
                }
            }

            return d;
            //throw new NotImplementedException();
        }
    }
}