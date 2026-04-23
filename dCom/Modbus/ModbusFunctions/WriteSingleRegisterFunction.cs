using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            byte[] frame = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.TransactionId)), 0, frame, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.ProtocolId)), 0, frame, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.Length)), 0, frame, 4, 2);

            frame[6] = CommandParameters.UnitId;
            frame[7] = CommandParameters.FunctionCode;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusWriteCommandParameters)CommandParameters).OutputAddress)), 0, frame, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)(((ModbusWriteCommandParameters)CommandParameters).Value))), 0, frame, 10, 2);

            return frame;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var map = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if (response[7] != (CommandParameters.FunctionCode + 0x80))
            {
                var addr = BitConverter.ToUInt16(response, 8);
                var val = BitConverter.ToUInt16(response, 10);

                addr = (ushort)IPAddress.NetworkToHostOrder((short)addr);
                val = (ushort)IPAddress.NetworkToHostOrder((short)val);

                map.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, addr), val);
            }
            else 
            {
                HandleException(response[8]);
            }

            return map;
        }
    }
}