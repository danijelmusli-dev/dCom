using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read holding registers functions/requests.
    /// </summary>
    public class ReadHoldingRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadHoldingRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadHoldingRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
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

            if ((response[7] & 0x80)!= 0)
            {
                HandleException(response[8]);
                return map;
            }

            var param = (ModbusReadCommandParameters)CommandParameters;

            int byteCount = response[8];
            ushort baseAddr = param.StartAddress;

            int registerCount = byteCount / 2;

            for(int i = 0; i < registerCount; i++)
            {
                int position = 9 + (i * 2);
                ushort posValue = (ushort)((response[position] << 8) | response[position + 1]);

                var tupleKey = new Tuple<PointType, ushort>(
                    PointType.ANALOG_OUTPUT, ((ushort)(baseAddr + i)));

                map[tupleKey] = posValue;
            }

            return map;
        }
    }
}