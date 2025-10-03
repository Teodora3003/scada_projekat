using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            //TO DO: IMPLEMENT
            ModbusWriteCommandParameters paramCom = this.CommandParameters as ModbusWriteCommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy(
                (Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(
                        (short)paramCom.TransactionId)), 0, (Array)request, 0, 2);

            Buffer.BlockCopy(
                (Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder((short)paramCom.ProtocolId)
                ),
                0,
                (Array)request,
                2,
                2
            );

            Buffer.BlockCopy(
                (Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder((short)paramCom.Length)
                ),
                0,
                (Array)request,
                4,
                2
            );

            request[6] = paramCom.UnitId;
            request[7] = paramCom.FunctionCode;

            Buffer.BlockCopy(
                (Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder((short)paramCom.OutputAddress)
                ),
                0,
                (Array)request,
                8,
                2
            );

            Buffer.BlockCopy(
                (Array)BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder((short)paramCom.Value)
                ),
                0,
                (Array)request,
                10,
                2
            );
            return request;
            //throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            //TO DO: IMPLEMENT
            {
                Dictionary<Tuple<PointType, ushort>, ushort> result = new Dictionary<Tuple<PointType, ushort>, ushort>();
                ModbusWriteCommandParameters parameter = (ModbusWriteCommandParameters)CommandParameters;
                //ushort address = BitConverter.ToUInt16(new byte[] { response[9], response[8] }, 0);
                ushort value = BitConverter.ToUInt16(new byte[] { response[11], response[10] }, 0);
                result.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, parameter.OutputAddress), value);
                return result;
            }
            //throw new NotImplementedException();
        }
    }
}