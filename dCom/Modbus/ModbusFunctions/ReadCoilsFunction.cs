using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

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
            byte[] frame = new byte[12];

            var param = (ModbusReadCommandParameters)CommandParameters;

            short transactionId = IPAddress.HostToNetworkOrder((short)CommandParameters.TransactionId);
            short protocolId = IPAddress.HostToNetworkOrder((short)CommandParameters.ProtocolId);
            short length = IPAddress.HostToNetworkOrder((short)CommandParameters.Length);

            short startAddr = IPAddress.HostToNetworkOrder((short)param.StartAddress);
            short quantity = IPAddress.HostToNetworkOrder((short)param.Quantity);

            Buffer.BlockCopy(BitConverter.GetBytes(transactionId), 0, frame, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(protocolId), 0, frame, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(length), 0, frame, 4, 2);

            frame[6] = CommandParameters.UnitId;
            frame[7] = CommandParameters.FunctionCode;

            Buffer.BlockCopy(BitConverter.GetBytes(startAddr), 0, frame, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(quantity), 0, frame, 10, 2);

            return frame;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var map = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if ((response[7] & 0x80) != 0)
            {
                HandleException(response[0]);
                return map;
            }

            var param = (ModbusReadCommandParameters)CommandParameters;

            int byteCount      = response[8];
            int remaining      = param.Quantity;
            ushort currentAddr = param.StartAddress;

            int byteIndex = 0;
            while ((byteIndex < byteCount) && (remaining > 0))
            { 
                byte currentByte = response[byteIndex + 9];

                int bitIndex = 0;
                while ((bitIndex < 8) && (remaining > 0))
                {
                    ushort bitValue = (ushort)(currentByte & 0x01);

                    var key = new Tuple<PointType, ushort>(
                        PointType.DIGITAL_OUTPUT, currentAddr);

                    map[key] = bitValue;

                    currentByte >>= 1;
                    currentAddr++;

                    bitIndex++;
                    remaining--;
                }

            }

            return map;
        }
    }
}